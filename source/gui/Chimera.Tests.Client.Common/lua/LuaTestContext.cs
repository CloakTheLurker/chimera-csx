using System.Collections.Generic;
using System.IO;
using System.Linq;

using Chimera.Client.Common;
using Chimera.Emulation.Common;
using Chimera.Tests.Client.Common.Movie;

namespace Chimera.Tests.Client.Common.lua
{
	internal class LuaTestContext
	{
		private static readonly string BASE_SCRIPT_PATH = Path.Combine(Environment.CurrentDirectory, "lua/scripts");

		private LuaLibraries lua;

		public List<string> loggedMessages = new();

		private FakeEmulator emulator = new();

		public LuaTestContext()
		{
			Action<string> print = loggedMessages.Add;
			FakeMainFormForApi mainApi = new();
			Config config = new();

			ApiContainer apiContainer = ApiManager.RestartLua(
				emulator.ServiceProvider,
				print,
				mainApi,
				null!,
				null!,
				null!,
				null!,
				config,
				emulator,
				new GameInfo(),
				null!);


			lua = new(
				new LuaFileList([ ], () => { }),
				emulator.ServiceProvider,
				mainApi,
				config,
				print,
				null!,
				apiContainer
			);
		}

		public void AddScript(string path, bool enable)
		{
			string absolutePath = Path.GetFullPath(Path.Combine(BASE_SCRIPT_PATH, path));
			LuaFile luaFile = new(absolutePath, () => { });
			lua.ScriptList.Add(luaFile);
			if (enable)
				luaFile.Start(lua.SpawnCoroutineAndSandbox(luaFile.Path));
		}

		public void StopScript(int id)
		{
			lua.ScriptList[id].Stop();
		}

		public void RunYielding()
		{
			lua.ResumeScripts(false);
		}

		public void RunFrameWaiting()
		{
			Controller c = new(emulator.ControllerDefinition);
			emulator.FrameAdvance(c, true);
			lua.CallFrameAfterEvent();
			lua.ResumeScripts(true);
		}

		/// <summary>run_lua: the agent's text run to its end, with a time limit (issue 571).</summary>
		public (object[] Results, string? Error) RunAgentText(string code, double seconds)
		{
			LuaFile file = new(Path.Combine(Environment.CurrentDirectory, "lua", "agent"), () => { });
			string? error = null;
			var results = lua.RunText(file, code, message => error = message, seconds);
			return (results, error);
		}

		private LuaFile _agentFrames;

		/// <summary>run_lua with frames: the agent's text started as a script that may wait for frames (issue 571).</summary>
		public (bool Waiting, object[] Results, string? Error) StartAgentTextAcrossFrames(string code, double seconds)
		{
			_agentFrames = new(Path.Combine(Environment.CurrentDirectory, "lua", "agent"), () => { });
			lua.ScriptList.Add(_agentFrames);
			string? error = null;
			var (waiting, results) = lua.RunTextAcrossFrames(_agentFrames, code, seconds, message => error = message);
			return (waiting, results, error);
		}

		/// <summary>run_lua with frames: the text once the frames are played; whether it ended on its own, and what it returned (issue 571).</summary>
		public (bool Ended, object[] Results, string? Error) FinishAgentTextAcrossFrames()
		{
			if (_agentFrames is not { } file) return (false, new object[0], null);
			var ended = !file.Enabled;
			var error = file.LastError;
			var results = ended && error is null ? lua.TakeAgentResults() : new object[0];
			lua.ScriptList.Remove(file);
			file.Stop();
			_agentFrames = null;
			return (ended, results, error);
		}

		/// <summary>As TAStudio and the Lua engine do when a branch is loaded.</summary>
		public void CallLoadStateEvent(string branchName)
		{
			lua.CallLoadStateEvent(branchName);
		}

		public void AssertLogMatches(params string[] messages)
		{
			Assert.AreEqual(messages.Length, loggedMessages.Count);
			for (int i = 0; i < messages.Length; i++)
			{
				Assert.AreEqual(messages[i], loggedMessages[i]);
			}
		}

		public LuaFile.RunState GetScriptState(int id)
		{
			return lua.ScriptList[id].State;
		}

		public List<NamedLuaFunction> FunctionsRegisteredToScript(int id)
		{
			return lua.ScriptList[id].Functions.ToList();
		}
	}
}
