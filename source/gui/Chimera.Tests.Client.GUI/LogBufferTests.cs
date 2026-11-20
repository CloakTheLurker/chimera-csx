using System;
using System.IO;

using Chimera.Client.GUI;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// The log belongs to the frontend, not to the window, so that it can run in
	/// the background: kept from startup, shown whenever the window is opened.
	/// These tests take the console over for a moment and give it back, which is
	/// why the whole assembly runs one test at a time.
	/// </summary>
	[TestClass]
	public class LogBufferTests
	{
		[TestMethod]
		public void LinesWrittenToTheConsoleAreKept()
		{
			LogBuffer log = new();
			var console = Console.Out;
			try
			{
				log.SetBackground(true);
				Console.WriteLine("a line the log should keep");
				Assert.AreEqual(1, log.Count, "the console line was not kept");
				Assert.AreEqual("a line the log should keep", log[0]);
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}

		[TestMethod]
		public void WithBackgroundOffTheLogOnlyRunsWhileTheWindowIsOpen()
		{
			LogBuffer log = new();
			var console = Console.Out;
			try
			{
				log.SetBackground(false);
				Console.WriteLine("written with no window open");
				Assert.AreEqual(0, log.Count, "the log kept a line with background recording off and no window open");

				log.ViewOpened();
				Console.WriteLine("written while the window is open");
				Assert.AreEqual(1, log.Count, "the log did not keep a line while its window was open");

				log.ViewClosed();
				Console.WriteLine("written after the window closed");
				Assert.AreEqual(1, log.Count, "the log kept a line after the window closed");
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}

		/// <summary>Turning background recording off while the window is open must
		/// not cut the log off under the person reading it.</summary>
		[TestMethod]
		public void TurningBackgroundOffWaitsForTheWindowToClose()
		{
			LogBuffer log = new();
			var console = Console.Out;
			try
			{
				log.SetBackground(true);
				log.ViewOpened();
				log.SetBackground(false);
				Assert.IsTrue(log.Capturing, "the log stopped while its window was still open");
				Console.WriteLine("still kept");
				Assert.AreEqual(1, log.Count);
				log.ViewClosed();
				Assert.IsFalse(log.Capturing, "the log kept running after its window closed");
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}

		/// <summary>An on-screen message is not written to the console, and reaches
		/// the log through the same buffer.</summary>
		[TestMethod]
		public void OnScreenMessagesGoToTheSameLog()
		{
			LogBuffer log = new();
			var console = Console.Out;
			try
			{
				log.SetBackground(true);
				log.Append("Speed: 100%");
				Assert.AreEqual(1, log.Count);
				Assert.AreEqual("Speed: 100%", log[0]);
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}

		/// <summary>Stopping hands the console back: a headless run's output is what
		/// it was before the log ever ran.</summary>
		[TestMethod]
		public void StoppingGivesTheConsoleBack()
		{
			LogBuffer log = new();
			var console = Console.Out;
			try
			{
				log.SetBackground(true);
				Assert.AreNotSame(console, Console.Out, "the log did not take the console");
				log.Stop();
				Assert.AreSame(console, Console.Out, "the log did not give the console back");
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}

		/// <summary>
		/// Holding the console must not hide the output from whoever started the
		/// process: the witness counts a core's boot line and waits for the MCP
		/// server's started line on it, and a person running a script does the
		/// same. Every line the log keeps goes on to the console it took.
		/// </summary>
		[TestMethod]
		public void TheConsoleStillGetsWhatTheLogKeeps()
		{
			LogBuffer log = new();
			var console = Console.Out;
			StringWriter outside = new();
			try
			{
				Console.SetOut(outside);
				log.SetBackground(true);
				Console.WriteLine("a line both places see");
				Assert.AreEqual(1, log.Count);
				Assert.AreEqual("a line both places see", log[0]);
				StringAssert.Contains(outside.ToString(), "a line both places see", "the console did not get the line");
			}
			finally
			{
				log.Stop();
				Console.SetOut(console);
			}
		}
	}
}
