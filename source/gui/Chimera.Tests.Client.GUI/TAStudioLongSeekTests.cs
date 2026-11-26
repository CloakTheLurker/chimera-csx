using Chimera.Client.GUI;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// The long seek's question (upstream issue 595): a seek that would replay
	/// for more than ten seconds asks first, so nobody is left watching a slow
	/// replay they did not ask for. But a replay of a handful of frames is not a
	/// long seek, however slowly the last one ran - a session's first seek can
	/// measure a fraction of a frame a second, and one frame then read as fifteen
	/// seconds and asked (user-decided, 2026-11-26).
	/// </summary>
	[TestClass]
	public class TAStudioLongSeekTests
	{
		/// <summary>About what a cold first seek measured: one frame in fifteen seconds.</summary>
		private const double VerySlow = 1.0 / 15;

		[TestMethod]
		public void AReplayTooShortToMatterNeverAsks()
		{
			Assert.IsFalse(TAStudio.SeekAsksFirst(1, VerySlow, headless: false),
				"one frame, even at a cold seek's speed, is not a long seek");
			Assert.IsFalse(TAStudio.SeekAsksFirst(TAStudio.FramesTooFewToAskAbout - 1, 0.01, headless: false),
				"a replay just under the floor asks nothing, however long it is estimated to take");
		}

		[TestMethod]
		public void ALongReplayStillAsks()
		{
			Assert.IsTrue(TAStudio.SeekAsksFirst(TAStudio.FramesTooFewToAskAbout, 0.5, headless: false),
				"thirty frames at half a frame a second is a minute of replay");
			Assert.IsTrue(TAStudio.SeekAsksFirst(1000, 50, headless: false));
		}

		[TestMethod]
		public void NothingIsAskedWithoutAMeasurementOrWhenHeadless()
		{
			Assert.IsFalse(TAStudio.SeekAsksFirst(1000, 0, headless: false), "no measurement yet, nothing to estimate from");
			Assert.IsFalse(TAStudio.SeekAsksFirst(1000, 50, headless: true), "a script has nobody to ask");
			Assert.IsFalse(TAStudio.SeekAsksFirst(TAStudio.FramesTooFewToAskAbout, 100, headless: false),
				"a replay the last seek's speed makes short is not long");
		}
	}
}
