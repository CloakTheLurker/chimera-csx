using System;
using System.IO;

using Chimera.Emulation.Common.Engine;

namespace Chimera.Tests.Emulation.Common.Engine
{
	/// <summary>
	/// The engine's file hashes, as New Project asks for them (issue 611): a
	/// file already hashed answers without reading, a read reports its way
	/// after every megabyte and can be stopped, and a stopped read is not
	/// remembered. The same test the engine runs natively (test_sha1.cpp),
	/// through the ABI the window calls.
	/// </summary>
	[TestClass]
	public class EngineFileHashTests
	{
		[TestMethod]
		public void AStoppedReadKeepsNothingAndAWholeOneIsKnownAfterward()
		{
			var path = Path.Combine(Path.GetTempPath(), $"chimera-hash-{Guid.NewGuid():N}.bin");
			try
			{
				using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					var block = new byte[1 << 20];
					for (var i = 0; i < 9; i++) stream.Write(block, 0, block.Length);
				}

				Assert.IsFalse(ChimeraEngine.KnownFileHash(path, out _, out _), "a file never read is not known");

				var calls = 0;
				ulong lastDone = 0, lastTotal = 0;
				var stopped = ChimeraEngine.HashFile(path, (done, total) =>
				{
					calls++;
					lastDone = done;
					lastTotal = total;
					return calls < 3;
				}, out var nothing, out _);
				Assert.AreEqual(ChimeraEngine.FileHashOutcome.Stopped, stopped);
				Assert.AreEqual(3, calls, "the caller is asked after every megabyte");
				Assert.AreEqual("", nothing, "a stopped read has no hash");
				Assert.IsFalse(ChimeraEngine.KnownFileHash(path, out _, out _), "and it is not remembered");

				calls = 0;
				var whole = ChimeraEngine.HashFile(path, (done, total) =>
				{
					calls++;
					lastDone = done;
					lastTotal = total;
					return true;
				}, out var sha1, out var length);
				Assert.AreEqual(ChimeraEngine.FileHashOutcome.Hashed, whole);
				Assert.AreEqual(9L << 20, length);
				Assert.IsTrue(calls >= 9, "one ask a megabyte at least");
				Assert.AreEqual((ulong)length, lastDone, "the last ask is the whole file");
				Assert.AreEqual((ulong)length, lastTotal);

				Assert.IsTrue(ChimeraEngine.KnownFileHash(path, out var known, out var knownLength), "a finished read is known");
				Assert.AreEqual(sha1, known);
				Assert.AreEqual(length, knownLength);

				var cachedCalls = 0;
				Assert.AreEqual(ChimeraEngine.FileHashOutcome.Hashed,
					ChimeraEngine.HashFile(path, (_, _) => { cachedCalls++; return true; }, out var again, out _));
				Assert.AreEqual(sha1, again);
				Assert.AreEqual(0, cachedCalls, "a file whose hash is known is not read again");

				Assert.AreEqual(ChimeraEngine.FileHashOutcome.Unreadable,
					ChimeraEngine.HashFile(path + ".missing", null, out _, out _));
			}
			finally
			{
				try { File.Delete(path); } catch (IOException) { }
			}
		}
	}
}
