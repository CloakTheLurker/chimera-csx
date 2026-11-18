using Chimera.Client.GUI;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// A package built after nightly-2026-10-27 names its settings and options by
	/// key and carries the words in languages/en.json. The wizard reads the
	/// declaration for its machine list and settings page, so it has to pass that
	/// table too; it used to read the declaration alone and show the keys.
	/// </summary>
	[TestClass]
	public class WizardLanguageTests
	{
		[TestMethod]
		public void ASettingsWordsComeFromThePackagesTable()
		{
			var cfg = NewProjectWizard.LoadConfig("""
				{
				  "coreName": "Keyed",
				  "systemId": "X",
				  "settings": [
				    {
				      "name": "machine",
				      "display": "setting.machine",
				      "description": "setting.machine.about",
				      "type": "enum",
				      "options": [ "one", "two" ],
				      "optionLabels": { "one": "option.one", "two": "option.two" }
				    }
				  ]
				}
				""", """
				{ "texts": {
				    "setting.machine": "Machine",
				    "setting.machine.about": "Which machine this is.",
				    "option.one": "First",
				    "option.two": "Second"
				} }
				""");

			Assert.IsNotNull(cfg);
			Assert.AreEqual("Machine", cfg.Settings![0].Display);
			Assert.AreEqual("Which machine this is.", cfg.Settings[0].Description);
			Assert.AreEqual("First", cfg.Settings[0].LabelFor("one"));
			Assert.AreEqual("Second", cfg.Settings[0].LabelFor("two"));
		}

		[TestMethod]
		public void APackageWithoutATableKeepsItsOwnWords()
		{
			var cfg = NewProjectWizard.LoadConfig("""
				{
				  "coreName": "Words",
				  "systemId": "X",
				  "settings": [
				    { "name": "region", "display": "Region", "description": "Which region.", "type": "string" }
				  ]
				}
				""", null);

			Assert.IsNotNull(cfg);
			Assert.AreEqual("Region", cfg.Settings![0].Display);
			Assert.AreEqual("Which region.", cfg.Settings[0].Description);
		}
	}
}
