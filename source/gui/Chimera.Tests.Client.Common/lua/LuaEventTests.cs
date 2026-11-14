using Chimera.Client.Common;

namespace Chimera.Tests.Client.Common.lua
{
	[DoNotParallelize]
	[TestClass]
	public class LuaEventTests
	{
		[TestMethod]
		public void OnLoadStateIsCalledWithTheBranchName()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("loadstate.lua", true);
			context.RunYielding();

			// act
			context.CallLoadStateEvent("branch A");

			// assert
			context.AssertLogMatches("loaded branch A");
		}

		[TestMethod]
		public void CanReplaceValueSaysNoForACoreThatOnlyReports()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("can_replace.lua", true);

			// act
			context.RunYielding();

			// assert: no core here watches memory, and "bad" is not a kind
			context.AssertLogMatches("false", "false", "false", "false");
		}

		/// <summary>run_lua with frames (issue 571): the agent's code may wait for frames, and what it returned comes back when it ends.</summary>
		[TestMethod]
		public void AgentTextMayWaitForFrames()
		{
			// arrange
			LuaTestContext context = new();
			var (waiting, _, error) = context.StartAgentTextAcrossFrames(
				"for i = 1, 3 do emu.frameadvance() print(i) end return 'done'", 10);
			Assert.IsTrue(waiting, error);

			// act: three frames are played for it
			for (var i = 0; i < 3; i++) context.RunFrameWaiting();

			// assert
			var (ended, results, failed) = context.FinishAgentTextAcrossFrames();
			Assert.IsTrue(ended, "the code ended after its three frames");
			Assert.IsNull(failed);
			CollectionAssert.AreEqual(new object[] { "done" }, results);
			context.AssertLogMatches("1", "2", "3");
		}

		/// <summary>run_lua's time limit (issue 571): a loop that never ends is stopped with an error.</summary>
		[TestMethod]
		public void AgentTextThatDoesNotEndIsStopped()
		{
			LuaTestContext context = new();
			var (results, error) = context.RunAgentText("while true do end", 1);
			Assert.AreEqual(0, results.Length);
			Assert.IsNotNull(error);
			StringAssert.Contains(error, "still running");
		}

		[TestMethod]
		public void ScriptWithRegisteredFunctionShowsAsRunning()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_frame_end.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0));
		}

		[TestMethod]
		public void UnregisteringLastEventStopsScript()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("unregister_event.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(LuaFile.RunState.Disabled, context.GetScriptState(0));
		}

		[TestMethod]
		public void ExitEventDoesNotKeepScriptActive()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_exit.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(LuaFile.RunState.Disabled, context.GetScriptState(0));
			context.AssertLogMatches("foo");
		}

		[TestMethod]
		public void ExceptionPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("exception.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void ExceptionInCallbackPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("callback_exception.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void ExceptionInCallbackDoesNotStopScript()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("callback_exception.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0));
		}

		// Lua itself should not know anything about the specialness of callbacks.
		// "special" callback here refers to events raised by C# callback systems like input polling, which use some different code
		[TestMethod]
		public void ExceptionInSpecialCallbackPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("input_poll_exception.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void CurrentDirectoryIsSet()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible.lua", true);

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_1.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInSpecialCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_2.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInCallbackRegisteredFromCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_3.lua", true);
			context.RunFrameWaiting();

			// act
			context.RunFrameWaiting();
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void ExitingScriptUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_exit.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}

		[TestMethod]
		public void StoppingScriptUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_frame_end.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.StopScript(0);

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}

		[TestMethod]
		public void ScriptStoppedByExceptionUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_then_exception.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}
	}
}
