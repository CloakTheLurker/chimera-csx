using System;
using System.IO;

using Chimera.Client.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chimera.Tests.Client.Common
{
	/// <summary>
	/// The idle screen used to be one on/off switch (the eye, or nothing) and is
	/// now four choices. A config written before that must keep the meaning it
	/// had: off stays off, on becomes the animated eye, and the old key is not
	/// written back.
	/// </summary>
	[TestClass]
	public class IdleScreenConfigTests
	{
		private static string TempFile()
			=> Path.Combine(Path.GetTempPath(), $"chimera-idle-{Guid.NewGuid():N}.ini");

		private static Config Load(string json)
		{
			var path = TempFile();
			File.WriteAllText(path, json);
			try
			{
				return ConfigService.Load<Config>(path);
			}
			finally
			{
				File.Delete(path);
			}
		}

		private static Config SaveAndLoad(Config config)
		{
			var path = TempFile();
			try
			{
				ConfigService.Save(path, config);
				return ConfigService.Load<Config>(path);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[TestMethod]
		public void EveryIdleScreenSurvivesASaveAndLoad()
		{
			foreach (var screen in new[] { EIdleScreen.Off, EIdleScreen.Eye, EIdleScreen.AnimatedEye, EIdleScreen.TvStatic })
			{
				Assert.AreEqual(screen, SaveAndLoad(new Config { IdleScreen = screen }).IdleScreen, $"{screen} did not come back");
			}
		}

		[TestMethod]
		public void AConfigWithTheEyeOffKeepsItOff()
			=> Assert.AreEqual(EIdleScreen.Off, Load("{\"ChimeraEyeScreensaver\": false}").IdleScreen);

		[TestMethod]
		public void AConfigWithTheEyeOnGetsTheAnimatedEye()
			=> Assert.AreEqual(EIdleScreen.AnimatedEye, Load("{\"ChimeraEyeScreensaver\": true}").IdleScreen);

		[TestMethod]
		public void AConfigThatNeverMentionedTheEyeStartsAnimated()
			=> Assert.AreEqual(EIdleScreen.AnimatedEye, Load("{}").IdleScreen);

		[TestMethod]
		public void TheOldSwitchIsNotWrittenBack()
		{
			var path = TempFile();
			try
			{
				Config config = Load("{\"ChimeraEyeScreensaver\": true}");
				ConfigService.Save(path, config);
				var text = File.ReadAllText(path);
				Assert.IsFalse(text.Contains("ChimeraEyeScreensaver"), "the old eye switch is still being written");
				Assert.IsTrue(text.Contains("IdleScreen"), "the idle screen choice was not written");
			}
			finally
			{
				File.Delete(path);
			}
		}
	}
}
