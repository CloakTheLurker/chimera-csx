using System.Collections.Generic;
using System.IO;
using System.Linq;

using Chimera.Client.Common;

using Newtonsoft.Json.Linq;

namespace Chimera.Tests.Client.Common.Movie
{
	/// <summary>
	/// The MCP server's tools of Chimera's main window (issue 355): pausing and running the machine,
	/// saving the project to a file the agent names, closing it, and saying where things are. Nothing
	/// they do may ask a question, so what would be asked is decided by the arguments: a file that is
	/// already there is kept unless 'overwrite' says otherwise, and unsaved changes are lost only when
	/// 'discard_unsaved' says they may be.
	/// </summary>
	[TestClass]
	public class McpMainWindowToolsTests
	{
		private sealed class FakeChimera : IMcpChimera
		{
			public int Frame { get; set; } = 5;
			public bool Paused { get; set; } = true;
			public readonly List<string> Saved = new();
			public int Closed;
			public string? SaveFails;

			public void Pause() => Paused = true;

			public void Resume() => Paused = false;

			public JObject ServerInfo() => new() { ["window"] = true };

			public string ProjectsFolder { get; set; } = "";

			public string? SaveProject(string path)
			{
				if (SaveFails is not null) return SaveFails;
				Saved.Add(path);
				File.WriteAllText(path, "saved");
				return null;
			}

			public string? CloseProject()
			{
				Closed++;
				Frame = -1;
				return null;
			}

			public McpProjectRequest? Asked;
			public McpProjectMade Made = new() { Opened = new JObject { ["core"] = "Synth" } };

			public McpProjectMade CreateProject(McpProjectRequest request)
			{
				Asked = request;
				return Made;
			}

			public IReadOnlyList<LibraryFunction>? Functions;

			public IReadOnlyList<LibraryFunction>? LuaFunctions() => Functions;

			public (object[] Results, string? Error, string Output) Ran = ([ ], null, "");
			public string? RanCode;
			public double RanSeconds;

			public (object[] Results, string? Error, string Output) RunLua(string code, double seconds)
			{
				RanCode = code;
				RanSeconds = seconds;
				return Ran;
			}

			public (string Path, bool Stop)? Script;
			public string? ScriptError;

			public (bool Running, string? Error, string Output) LuaScript(string path, bool stop)
			{
				Script = (path, stop);
				return (!stop && ScriptError is null, stop ? null : ScriptError, stop ? "stopped\n" : "started\n");
			}

			public McpLuaConsolePage Page = new();
			public (long Since, int Limit)? ConsoleAsked;

			public McpLuaConsolePage LuaConsole(long since, int limit)
			{
				ConsoleAsked = (since, limit);
				return Page;
			}

			public IReadOnlyList<McpLuaScript> Scripts = [ ];

			public IReadOnlyList<McpLuaScript> LuaScripts() => Scripts;

			public (bool Waiting, object[] Results, string? Error, string Output) Started = (false, [ ], null, "");
			public (bool Ended, object[] Results, string? Error, string Output) Finished = (true, [ ], null, "");
			public (string Code, double Seconds)? StartedFrames;
			public int Finishes;

			public (bool Waiting, object[] Results, string? Error, string Output) StartLuaFrames(string code, double seconds)
			{
				StartedFrames = (code, seconds);
				return Started;
			}

			public (bool Ended, object[] Results, string? Error, string Output) FinishLuaFrames()
			{
				Finishes++;
				return Finished;
			}
		}

		/// <summary>The window the tools ask things of, with nothing behind it but a record of what was asked.</summary>
		private sealed class FakeWindow : IMcpWindow
		{
			public int Frame { get; set; }
			public int SoughtTo = -1;
			public int PlayingTo = -1;

			public void SeekTo(int frame) => SoughtTo = frame;

			public int SaveBranch(string note) => 1;

			public void LoadBranch(int number)
			{
			}

			public bool SetCondition(string argumentsJson, out string why)
			{
				why = "";
				return true;
			}

			public bool ConditionMet { get; set; }

			public void PlayUntil(int frame) => PlayingTo = frame;

			public int PlayedFrames = -1;

			public void PlayFrames(int frames) => PlayedFrames = frames;

			public void TakePictures(IReadOnlyList<int> frames)
			{
			}
		}

		private string _folder = "";

		[TestInitialize]
		public void AFolderOfItsOwn() => Directory.CreateDirectory(_folder = Path.Combine(Path.GetTempPath(), "chimera-mcp-main-" + Path.GetRandomFileName()));

		[TestCleanup]
		public void Gone() => Directory.Delete(_folder, recursive: true);

		private static (bool IsError, string Text) Run(ITasMovie? movie, IMcpChimera chimera, string tool, string arguments = "{}")
		{
			var text = McpHostTools.Run(movie, null, tool, arguments, out var isError, chimera)!;
			return (isError, text);
		}

		private static (bool IsError, string Text) Run(ITasMovie? movie, IMcpWindow window, IMcpChimera chimera, string tool, string arguments = "{}")
		{
			var text = McpHostTools.Run(movie, window, tool, arguments, out var isError, chimera)!;
			return (isError, text);
		}

		[TestMethod]
		public void PauseAndResumeAnswerWhereTheMachineIs()
		{
			FakeChimera chimera = new() { Paused = false, Frame = 42 };
			var (isError, text) = Run(TasMovieTests.MakeMovie(3), chimera, "pause");
			Assert.IsFalse(isError, text);
			Assert.IsTrue((bool) JObject.Parse(text)["paused"]!);
			Assert.AreEqual(42, (int) JObject.Parse(text)["frame"]!);
			(isError, text) = Run(TasMovieTests.MakeMovie(3), chimera, "resume");
			Assert.IsFalse(isError, text);
			Assert.IsFalse((bool) JObject.Parse(text)["paused"]!);
			Assert.IsFalse(chimera.Paused);

			// with no machine there is nothing to pause
			(isError, text) = Run(null, new FakeChimera { Frame = -1 }, "pause");
			Assert.IsTrue(isError);
			Assert.AreEqual(McpHostTools.NoMovie, text);
		}

		[TestMethod]
		public void ServerInfoNeedsNoProject()
		{
			var (isError, text) = Run(null, new FakeChimera { Frame = -1 }, "server_info");
			Assert.IsFalse(isError, text);
			Assert.IsTrue((bool) JObject.Parse(text)["window"]!);
		}

		[TestMethod]
		public void ANameIsSavedInTheMoviesFolderWithTheProjectsExtension()
		{
			var movie = TasMovieTests.MakeMovie(3);
			FakeChimera chimera = new() { ProjectsFolder = _folder };
			var (isError, text) = Run(movie, chimera, "save_project", """{ "path": "Ninja Gaiden (U) agent" }""");
			Assert.IsFalse(isError, text);
			var expected = Path.Combine(_folder, "Ninja Gaiden (U) agent.chimeraProject");
			CollectionAssert.AreEqual(new[] { expected }, chimera.Saved);
			Assert.AreEqual(expected, (string) JObject.Parse(text)["saved"]!);
		}

		[TestMethod]
		public void AFileThatIsThereIsKeptUnlessOverwriteIsGiven()
		{
			var movie = TasMovieTests.MakeMovie(3);
			FakeChimera chimera = new() { ProjectsFolder = _folder };
			var there = Path.Combine(_folder, "there.chimeraProject");
			File.WriteAllText(there, "the person's own");

			var (isError, text) = Run(movie, chimera, "save_project", new JObject { ["path"] = there }.ToString());
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "overwrite: true");
			Assert.AreEqual(0, chimera.Saved.Count);
			Assert.AreEqual("the person's own", File.ReadAllText(there), "nothing was written");

			(isError, text) = Run(movie, chimera, "save_project", new JObject { ["path"] = there, ["overwrite"] = true }.ToString());
			Assert.IsFalse(isError, text);
			CollectionAssert.AreEqual(new[] { there }, chimera.Saved);
		}

		[TestMethod]
		public void WithNoPathTheProjectIsSavedWhereItIs()
		{
			var movie = TasMovieTests.MakeMovie(3);
			FakeChimera chimera = new() { ProjectsFolder = _folder };
			// never saved: there is nowhere to save it to
			var (isError, text) = Run(movie, chimera, "save_project");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "give 'path'");

			// its own file is no file of someone else's: saving over it needs no 'overwrite'
			var own = Path.Combine(_folder, "own.chimeraProject");
			File.WriteAllText(own, "before");
			movie.Filename = own;
			(isError, text) = Run(movie, chimera, "save_project");
			Assert.IsFalse(isError, text);
			(isError, text) = Run(movie, chimera, "save_project", new JObject { ["path"] = own }.ToString());
			Assert.IsFalse(isError, text);
			CollectionAssert.AreEqual(new[] { own, own }, chimera.Saved);
		}

		[TestMethod]
		public void AFolderThatIsNotThereOrAFailedSaveIsSaid()
		{
			var movie = TasMovieTests.MakeMovie(3);
			FakeChimera chimera = new() { ProjectsFolder = _folder };
			var (isError, text) = Run(movie, chimera, "save_project", new JObject { ["path"] = Path.Combine(_folder, "no such folder", "p") }.ToString());
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "does not exist");

			chimera.SaveFails = "the disk is full";
			(isError, text) = Run(movie, chimera, "save_project", """{ "path": "p" }""");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "the disk is full");
		}

		[TestMethod]
		public void UnsavedChangesAreLostOnlyWhenTheAgentSaysSo()
		{
			var movie = TasMovieTests.MakeMovie(3);
			Assert.IsTrue(movie.Changes, "a movie that was just made has changes nobody saved");
			FakeChimera chimera = new();

			var (isError, text) = Run(movie, chimera, "close_tastudio");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "discard_unsaved: true");
			Assert.AreEqual(0, chimera.Closed);
			Assert.IsTrue(movie.Changes, "nothing was thrown away");

			(isError, text) = Run(movie, chimera, "close_tastudio", """{ "discard_unsaved": true }""");
			Assert.IsFalse(isError, text);
			Assert.AreEqual(1, chimera.Closed);
			Assert.IsFalse(movie.Changes, "cleared first, so closing has nothing to ask");
			var answer = JObject.Parse(text);
			Assert.IsTrue((bool) answer["closed"]!);
			Assert.IsFalse((bool) answer["machine_open"]!);
			StringAssert.Contains((string) answer["note"]!, "lost");
		}

		[TestMethod]
		public void CreateProjectHandsTheWindowTheAgentsAnswers()
		{
			FakeChimera chimera = new();
			var game = Path.Combine(_folder, "game.nes");
			File.WriteAllText(game, "a game");
			var (isError, text) = Run(null, chimera, "create_project", new JObject
			{
				["core"] = " Synth ",
				["system"] = "NES",
				["game"] = game,
				["files"] = new JObject { ["disk"] = new JArray(game, game) },
				["settings"] = new JObject { ["region"] = "pal", ["seed"] = 4000000000L },
				["firmware"] = new JObject { ["bios"] = game },
				["firmware_folder"] = _folder,
			}.ToString());
			Assert.IsFalse(isError, text);
			Assert.AreEqual("Synth", (string) JObject.Parse(text)["core"]!, "the window's answer is the agent's");
			var asked = chimera.Asked!;
			Assert.AreEqual("Synth", asked.Core);
			Assert.AreEqual("NES", asked.System);
			CollectionAssert.AreEqual(new[] { game }, asked.Games.ToArray());
			CollectionAssert.AreEqual(new[] { ("disk", game), ("disk", game) }, asked.Files.ToArray());
			Assert.AreEqual("pal", asked.Settings["region"]);
			Assert.AreEqual(4000000000L, asked.Settings["seed"]);
			CollectionAssert.AreEqual(new[] { ("bios", game) }, asked.Firmware.ToArray());
			Assert.AreEqual(_folder, asked.FirmwareFolder);
			Assert.IsFalse(asked.DiscardUnsaved);
		}

		[TestMethod]
		public void AProjectWithUnsavedChangesIsNotReplacedUnlessTheAgentSaysSo()
		{
			var movie = TasMovieTests.MakeMovie(3);
			FakeChimera chimera = new();
			var (isError, text) = Run(movie, chimera, "create_project", """{ "core": "Synth" }""");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "discard_unsaved: true");
			Assert.IsNull(chimera.Asked, "the window was not asked");

			(isError, text) = Run(movie, chimera, "create_project", """{ "core": "Synth", "discard_unsaved": true }""");
			Assert.IsFalse(isError, text);
			Assert.IsTrue(chimera.Asked!.DiscardUnsaved, "the window lets them go once the new project is made, not before");
			Assert.IsTrue(movie.Changes);
		}

		[TestMethod]
		public void MissingFirmwareIsAnAnswerOfItsOwn()
		{
			FakeChimera chimera = new()
			{
				Made = new()
				{
					Refusal = "Firmware the project needs was not found: Boot ROM.",
					Missing = new JArray(new JObject { ["id"] = "bios", ["name"] = "Boot ROM" }),
					Searched = new JArray(_folder),
				},
			};
			var (isError, text) = Run(null, chimera, "create_project", """{ "core": "Synth" }""");
			Assert.IsTrue(isError);
			var answer = JObject.Parse(text);
			Assert.AreEqual("missing_firmware", (string) answer["error"]!);
			Assert.AreEqual("bios", (string) answer["missing"]![0]!["id"]!);
			Assert.AreEqual(_folder, (string) answer["searched"]![0]!);

			chimera.Made = new() { Refusal = "There is no core called 'Nope'." };
			(isError, text) = Run(null, chimera, "create_project", """{ "core": "Nope" }""");
			Assert.IsTrue(isError);
			Assert.AreEqual("There is no core called 'Nope'.", text);
		}

		[TestMethod]
		public void CreateProjectArgumentsOfTheWrongShapeAreRefused()
		{
			FakeChimera chimera = new();
			foreach (var (arguments, said) in new[]
			{
				("{}", "Give 'core'"),
				("""{ "core": "Synth", "files": ["game.nes"] }""", "'files' is an object"),
				("""{ "core": "Synth", "game": 5 }""", "'game' is a path"),
				("""{ "core": "Synth", "settings": { "region": [1] } }""", "'settings' is an object"),
				("""{ "core": "Synth", "firmware": { "bios": 1 } }""", "'firmware' is an object"),
				("""{ "core": "Synth", "firmware_folder": "/no/such/folder" }""", "There is no folder"),
			})
			{
				var (isError, text) = Run(null, chimera, "create_project", arguments);
				Assert.IsTrue(isError, arguments);
				StringAssert.Contains(text, said, arguments);
			}
			Assert.IsNull(chimera.Asked, "nothing reached the window");
		}

		/// <summary>The functions lua_functions lists in these tests, as a library declares them.</summary>
		private static class Emu
		{
			[LuaMethod("framecount", "The frame the machine is on.")]
			[LuaMethodExample("local frame = emu.framecount();")]
			public static int FrameCount() => 0;

			[LuaMethod("yield", "Waits for the next frame; for scripts.")]
			public static void Yield()
			{
			}
		}

		private static class Memory
		{
			[LuaMethod("read_u8", "Reads one byte.")]
			public static uint ReadByte(long address, string? domain = null) => 0;
		}

		private static IReadOnlyList<LibraryFunction> Functions()
			=>
			[
				new("emu", "", typeof(Emu).GetMethod(nameof(Emu.FrameCount))!),
				new("emu", "", typeof(Emu).GetMethod(nameof(Emu.Yield))!),
				new("memory", "", typeof(Memory).GetMethod(nameof(Memory.ReadByte))!),
			];

		/// <summary>Every Lua function is offered through the server (issue 355): lua_functions lists them as the Lua Console documents them.</summary>
		[TestMethod]
		public void TheLuaFunctionsAreListedByLibraryOrByWords()
		{
			FakeChimera chimera = new() { Functions = Functions() };
			var all = JObject.Parse(Run(null, chimera, "lua_functions").Text);
			Assert.AreEqual(3, (int) all["total"]!);
			CollectionAssert.AreEqual(new[] { "emu", "memory" }, all["libraries"]!.Select(static l => (string) l!).ToArray());
			var first = all["functions"]![0]!;
			Assert.AreEqual("emu.framecount", (string) first["name"]!);
			Assert.AreEqual("The frame the machine is on.", (string) first["description"]!);
			Assert.AreEqual("local frame = emu.framecount();", (string) first["example"]!);
			StringAssert.Contains((string) all["functions"]![2]!["parameters"]!, "address");

			var memory = JObject.Parse(Run(null, chimera, "lua_functions", """{ "library": "memory" }""").Text);
			Assert.AreEqual("memory.read_u8", (string) memory["functions"]!.Single()["name"]!);
			var byWords = JObject.Parse(Run(null, chimera, "lua_functions", """{ "search": "NEXT FRAME" }""").Text);
			Assert.AreEqual("emu.yield", (string) byWords["functions"]!.Single()["name"]!);
			var one = JObject.Parse(Run(null, chimera, "lua_functions", """{ "limit": 1 }""").Text);
			Assert.AreEqual(3, (int) one["total"]!, "the total counts them all");
			Assert.AreEqual(1, one["functions"]!.Count());
		}

		[TestMethod]
		public void LuaRunsAndWhatItReturnedAndPrintedComesBack()
		{
			Assert.IsTrue(LuaLibraries.IsAvailable, "the Lua library loads");
			using NLua.Lua lua = new();
			var returned = lua.DoString("return 3, 2.5, 'x', true, nil, {1, 2, 3}, {a = 1, b = {c = 'd'}}, function() end");
			FakeChimera chimera = new() { Functions = Functions(), Ran = (returned, null, "printed\n") };
			var (isError, text) = Run(null, chimera, "run_lua", """{ "code": "return emu.framecount()" }""");
			Assert.IsFalse(isError, text);
			Assert.AreEqual("return emu.framecount()", chimera.RanCode);
			var answer = JObject.Parse(text);
			Assert.AreEqual("""[3,2.5,"x",true,null,[1,2,3],{"a":1,"b":{"c":"d"}},"(a function)"]""",
				answer["results"]!.ToString(Newtonsoft.Json.Formatting.None), "a whole number as one, a table as a list or an object");
			Assert.AreEqual("printed\n", (string) answer["output"]!);

			chimera.Ran = ([ ], "attempt to call a nil value (field 'nope')", "");
			(isError, text) = Run(null, chimera, "run_lua", """{ "code": "emu.nope()" }""");
			Assert.IsTrue(isError);
			StringAssert.Contains((string) JObject.Parse(text)["error"]!, "nil value");

			(isError, text) = Run(null, chimera, "run_lua", """{ "code": " " }""");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "Give 'code'");
			(isError, text) = Run(null, new FakeChimera(), "run_lua", """{ "code": "return 1" }""");
			Assert.IsTrue(isError, "with no Lua there is nothing to run it");
			StringAssert.Contains(text, "Lua cannot run");
		}

		[TestMethod]
		public void AScriptFileIsStartedAndStopped()
		{
			FakeChimera chimera = new() { Functions = Functions() };
			var script = Path.Combine(_folder, "watch.lua");
			File.WriteAllText(script, "print('started')");
			var started = JObject.Parse(Run(null, chimera, "lua_script", new JObject { ["path"] = script }.ToString()).Text);
			Assert.IsTrue((bool) started["running"]!);
			Assert.AreEqual((script, false), chimera.Script);
			var stopped = JObject.Parse(Run(null, chimera, "lua_script", new JObject { ["path"] = script, ["stop"] = true }.ToString()).Text);
			Assert.IsFalse((bool) stopped["running"]!);
			Assert.AreEqual("stopped\n", (string) stopped["output"]!);
			var (isError, text) = Run(null, chimera, "lua_script", new JObject { ["path"] = Path.Combine(_folder, "nope.lua") }.ToString());
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "There is no file");
		}

		/// <summary>lua_console (issue 571): the numbered lines from where the agent asks, and the number to ask from next.</summary>
		[TestMethod]
		public void TheLuaConsoleLinesAreNumberedAndSaidFromWhereToAskNext()
		{
			FakeChimera chimera = new()
			{
				Functions = Functions(),
				Page = new() { First = 3, Lines = ["three", "four"], Next = 5, Total = 9, Dropped = 3 },
			};
			var answer = JObject.Parse(Run(null, chimera, "lua_console", """{ "since": 0, "limit": 5 }""").Text);
			Assert.IsNotNull(chimera.ConsoleAsked);
			Assert.AreEqual(0L, chimera.ConsoleAsked!.Value.Since);
			Assert.AreEqual(5, chimera.ConsoleAsked.Value.Limit);
			Assert.AreEqual(3, (int) answer["first"]!);
			CollectionAssert.AreEqual(new[] { "three", "four" }, answer["lines"]!.Select(static l => (string) l!).ToArray());
			Assert.AreEqual(5, (int) answer["next"]!);
			Assert.AreEqual(9, (int) answer["total"]!);
			Assert.AreEqual(3, (int) answer["dropped"]!);

			var (isError, text) = Run(null, chimera, "lua_console", """{ "limit": 0 }""");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "'limit'");
		}

		/// <summary>lua_scripts (issue 571): each script of the console's list, its state and the error it last stopped on.</summary>
		[TestMethod]
		public void TheLuaScriptsAreListedWithTheirStateAndLastError()
		{
			FakeChimera chimera = new()
			{
				Functions = Functions(),
				Scripts =
				[
					new() { Name = "watch", Path = "/lua/watch.lua", State = "running" },
					new() { Name = "broken", Path = "/lua/broken.lua", State = "stopped", LastError = "input:2: nope" },
				],
			};
			var answer = JObject.Parse(Run(null, chimera, "lua_scripts").Text);
			var scripts = (JArray) answer["scripts"]!;
			Assert.AreEqual(2, scripts.Count);
			Assert.AreEqual("running", (string) scripts[0]["state"]!);
			Assert.IsNull(scripts[0]["last_error"]);
			Assert.AreEqual("stopped", (string) scripts[1]["state"]!);
			Assert.AreEqual("input:2: nope", (string) scripts[1]["last_error"]!);
		}

		/// <summary>lua_script (issue 571): a script that failed to start says the error with its line.</summary>
		[TestMethod]
		public void AScriptThatFailedToStartSaysItsError()
		{
			var script = Path.Combine(_folder, "broken.lua");
			File.WriteAllText(script, "nope()");
			FakeChimera chimera = new() { Functions = Functions(), ScriptError = "input:1: attempt to call a nil value" };
			var answer = JObject.Parse(Run(null, chimera, "lua_script", new JObject { ["path"] = script }.ToString()).Text);
			Assert.IsFalse((bool) answer["running"]!);
			Assert.AreEqual("input:1: attempt to call a nil value", (string) answer["error"]!);
		}

		/// <summary>run_lua (issue 571): a time limit goes with it, and with frames it plays them and answers after.</summary>
		[TestMethod]
		public void RunLuaTakesATimeLimit()
		{
			FakeChimera chimera = new() { Functions = Functions(), Ran = ([1L], null, "") };
			Run(null, chimera, "run_lua", """{ "code": "return 1", "seconds": 3 }""");
			Assert.AreEqual("return 1", chimera.RanCode);
			Assert.AreEqual(3.0, chimera.RanSeconds);
			Run(null, chimera, "run_lua", """{ "code": "return 1" }""");
			Assert.AreEqual(10.0, chimera.RanSeconds, "ten seconds when left out");

			var (isError, text) = Run(null, chimera, "run_lua", """{ "code": "return 1", "seconds": 0 }""");
			Assert.IsTrue(isError);
			StringAssert.Contains(text, "'seconds'");
		}

		/// <summary>run_lua with frames (issue 571): the code is started across frames and the call is answered after them.</summary>
		[TestMethod]
		public void RunLuaWithFramesPlaysThemAndAnswersAfterwards()
		{
			var movie = TasMovieTests.MakeMovie(10);
			FakeWindow window = new() { Frame = 3 };
			FakeChimera chimera = new()
			{
				Functions = Functions(),
				Started = (true, [ ], null, "printed\n"),
				Finished = (true, [7L], null, "printed\n"),
			};

			// waiting for frames: no answer yet, and the window is asked to play them
			var answer = McpHostTools.Run(movie, window, "run_lua", """{ "code": "emu.frameadvance() return 7", "frames": 4 }""",
				out var isError, chimera);
			Assert.IsNull(answer, "answered once the frames are played");
			Assert.IsFalse(isError);
			Assert.IsNotNull(chimera.StartedFrames);
			Assert.AreEqual(("emu.frameadvance() return 7", 14.0), chimera.StartedFrames!.Value, "ten seconds and one a frame");
			Assert.AreEqual(4, window.PlayedFrames, "four frames from where the machine is");

			// the frames are played: answered then
			var done = JObject.Parse(McpHostTools.LuaFramesAnswer(window, 4, true, [7L], null, "printed\n"));
			Assert.AreEqual(4, (int) done["frames_played"]!);
			Assert.IsTrue((bool) done["ended"]!);
			Assert.AreEqual(7, (int) done["results"]![0]!);
			Assert.AreEqual("printed\n", (string) done["output"]!);

			// code that ends at once needs no frame
			chimera.Started = (false, [7L], null, "");
			answer = McpHostTools.Run(movie, window, "run_lua", """{ "code": "return 7", "frames": 4 }""", out isError, chimera);
			Assert.IsFalse(isError);
			var atOnce = JObject.Parse(answer);
			Assert.AreEqual(0, (int) atOnce["frames_played"]!);
			Assert.IsTrue((bool) atOnce["ended"]!);
			Assert.AreEqual(7, (int) atOnce["results"]![0]!);
		}

		/// <summary>seek with extend (issue 571): empty frames are added to reach past the end, as one undoable step.</summary>
		[TestMethod]
		public void SeekWithExtendAddsFramesAsOneUndoableStep()
		{
			var movie = TasMovieTests.MakeMovie(10);
			FakeWindow window = new() { Frame = 2 };
			Assert.IsNull(McpHostTools.Run(movie, window, "seek", """{ "frame": 15, "extend": true }""", out var isError));
			Assert.IsFalse(isError);
			Assert.AreEqual(15, movie.InputLogLength);
			Assert.AreEqual(15, window.SoughtTo);
			movie.ChangeLog.Undo();
			Assert.AreEqual(10, movie.InputLogLength, "one undo takes the frames back");

			var (refused, text) = Run(movie, window, new FakeChimera(), "seek", """{ "frame": 15 }""");
			Assert.IsTrue(refused);
			StringAssert.Contains(text, "extend: true");
		}

		[TestMethod]
		public void ASavedProjectClosesWithNothingToSay()
		{
			var movie = TasMovieTests.MakeMovie(3);
			movie.ClearChanges();
			FakeChimera chimera = new();
			var (isError, text) = Run(movie, chimera, "close_tastudio");
			Assert.IsFalse(isError, text);
			Assert.AreEqual(1, chimera.Closed);
			Assert.IsNull(JObject.Parse(text)["note"]);
		}
	}
}
