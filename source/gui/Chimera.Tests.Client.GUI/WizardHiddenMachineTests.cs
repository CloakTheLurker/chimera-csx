using System.Linq;

using Chimera.Client.GUI;
using Chimera.Emulation.Common.Waterbox;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// A machine a package still declares but marks hidden is not offered for a
	/// NEW project. It stays a legal value of the machine setting, and opening a
	/// project that already names it puts it back in the picker, so re-saving the
	/// project cannot silently move it to another machine.
	/// </summary>
	[TestClass]
	public class WizardHiddenMachineTests
	{
		/// <summary>Ares' shape around the Atari 2600: declared, hidden, still legal.</summary>
		private static WaterboxConfig TwoMachines() => new()
		{
			MachineSetting = "machine",
			Machines =
			[
				new() { Id = "N64", Label = "Nintendo 64", When = [ "n64" ] },
				new() { Id = "A26", Label = "Atari 2600", When = [ "a26" ], Hidden = true },
			],
			Settings =
			[
				new WaterboxConfig.SettingDecl
				{
					Name = "machine",
					Type = "enum",
					Options = [ "n64", "a26" ],
					Default = "n64",
				},
			],
		};

		[TestMethod]
		public void AHiddenMachineIsNotOffered()
		{
			using var form = new NewProjectWizard([ ], static _ => [ ]);
			form.Show();
			form.UseMachinesFrom(TwoMachines());

			CollectionAssert.AreEqual(new[] { "Nintendo 64" }, form.OfferedMachines.ToList(),
				"New Project must not offer a machine the package marks hidden");
			Assert.AreEqual("N64", form.ChosenMachine);
			var pinned = form.SettingValue("machine");
			Assert.IsNotNull(pinned);
			Assert.AreEqual("n64", pinned.ToString());
		}

		[TestMethod]
		public void AProjectThatNamesAHiddenMachineKeepsIt()
		{
			using var form = new NewProjectWizard([ ], static _ => [ ]);
			form.Show();
			form.UseMachinesFrom(TwoMachines());
			form.SelectMachine("a26");

			Assert.AreEqual("A26", form.ChosenMachine,
				"an existing project's machine has to survive being opened and re-saved");
			var pinned = form.SettingValue("machine");
			Assert.IsNotNull(pinned);
			Assert.AreEqual("a26", pinned.ToString());
			CollectionAssert.Contains(form.OfferedMachines.ToList(), "Atari 2600");
		}

		[TestMethod]
		public void AHiddenMachineClaimsNoExtensions()
		{
			var cfg = TwoMachines();
			var machines = cfg.Machines!;
			machines[0].Extensions = new() { [".n64"] = "N64" };
			machines[1].Extensions = new() { [".a26"] = "A26" };

			CollectionAssert.DoesNotContain(cfg.AllExtensions.Keys.ToList(), ".a26",
				"opening the file directly would make a new project on a hidden machine");
			Assert.AreEqual("N64", cfg.AllExtensions[".n64"]);
		}

		[TestMethod]
		public void AnAgentCannotMakeANewProjectOnAHiddenMachine()
		{
			var machine = NewProjectWizard.AgentMachine(TwoMachines(), "a26", "ares", out var refusal);
			Assert.IsNull(machine, "an agent's new project must not land on a hidden machine");
			Assert.IsNotNull(refusal);
			StringAssert.Contains(refusal, "not offered for a new project");

			var visible = NewProjectWizard.AgentMachine(TwoMachines(), "n64", "ares", out var none);
			Assert.IsNull(none);
			Assert.IsNotNull(visible);
			Assert.AreEqual("N64", visible.Id);
		}
	}
}
