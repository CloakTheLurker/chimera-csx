using Chimera.Client.Common;
using Chimera.Emulation.Common.Waterbox;

namespace Chimera.Tests.Client.Common
{
	/// <summary>
	/// Packages built after nightly-2026-10-27 name settings, file slots and
	/// firmware by key and carry the words in languages/en.json. A package built
	/// before that holds the words itself and has no table; both must show a
	/// person words, never a key, and a key with no entry keeps the key.
	/// </summary>
	[TestClass]
	public class CoreLanguageTableTests
	{
		private const string EnglishTable = """
			{ "texts": {
				"setting.machine.display": "Machine",
				"setting.machine.description": "Which computer this is.",
				"setting.machine.option.128k": "ZX Spectrum 128K",
				"firmware.sos48.display": "48K ROM",
				"firmware.sos48.description": "The 48K Spectrum's ROM.",
				"slot.game.title": "Game",
				"slot.game.help": "The TAP or snapshot to run."
			} }
			""";

		[TestMethod]
		public void AKeyedConfigGetsThePackagesWords()
		{
			var cfg = WaterboxConfig.FromJson("""
				{
				  "coreName": "unrealspeccyp",
				  "settings": [
				    {
				      "name": "machine",
				      "display": "setting.machine.display",
				      "description": "setting.machine.description",
				      "type": "enum",
				      "options": [ "128k" ],
				      "optionLabels": { "128k": "setting.machine.option.128k" }
				    }
				  ],
				  "firmware": [
				    { "id": "sos48", "display": "firmware.sos48.display", "description": "firmware.sos48.description" }
				  ]
				}
				""", CoreLanguageTable.Parse(EnglishTable));

			Assert.IsNotNull(cfg);
			Assert.AreEqual("Machine", cfg.Settings![0].Display);
			Assert.AreEqual("Which computer this is.", cfg.Settings[0].Description);
			Assert.AreEqual("ZX Spectrum 128K", cfg.Settings[0].LabelFor("128k"));
			Assert.AreEqual("48K ROM", cfg.Firmware![0].DisplayName);
			Assert.AreEqual("The 48K Spectrum's ROM.", cfg.Firmware[0].Description);
		}

		[TestMethod]
		public void AnOldPackageKeepsTheWordsItHolds()
		{
			var cfg = WaterboxConfig.FromJson("""
				{ "settings": [ { "name": "machine", "display": "Machine", "description": "Which one." } ] }
				""");
			Assert.IsNotNull(cfg);
			Assert.AreEqual("Machine", cfg.Settings![0].Display);
			Assert.AreEqual("Which one.", cfg.Settings[0].Description);
		}

		[TestMethod]
		public void AKeyWithNoEntryKeepsTheKey()
		{
			var cfg = WaterboxConfig.FromJson("""
				{ "settings": [ { "name": "machine", "display": "setting.unknown.display" } ] }
				""", CoreLanguageTable.Parse(EnglishTable));
			Assert.IsNotNull(cfg);
			Assert.AreEqual("setting.unknown.display", cfg.Settings![0].Display);
		}

		[TestMethod]
		public void AFileSlotGetsThePackagesWords()
		{
			var declaration = ProjectSlotDeclaration.Parse("""
				{ "slots": [ { "id": "game", "title": "slot.game.title", "help": "slot.game.help" } ] }
				""", CoreLanguageTable.Parse(EnglishTable));
			Assert.IsNotNull(declaration);
			Assert.AreEqual("Game", declaration.Slots[0].Title);
			Assert.AreEqual("The TAP or snapshot to run.", declaration.Slots[0].Help);
		}

		[TestMethod]
		public void AnUnreadableTableLeavesTheDeclarationsAlone()
		{
			Assert.IsNull(CoreLanguageTable.Parse("not json"));
			Assert.IsNull(CoreLanguageTable.Parse("""{ "nothing": true }"""));
			var cfg = WaterboxConfig.FromJson("""
				{ "settings": [ { "name": "machine", "display": "setting.machine.display" } ] }
				""", CoreLanguageTable.Parse("not json"));
			Assert.IsNotNull(cfg);
			Assert.AreEqual("setting.machine.display", cfg.Settings![0].Display);
		}
	}
}
