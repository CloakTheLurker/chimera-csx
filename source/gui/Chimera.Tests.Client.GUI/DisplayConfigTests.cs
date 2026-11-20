using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Chimera.Client.Common;
using Chimera.Client.GUI;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// The idle screen's four choices in Config &gt; Display &gt; Misc: off, the
	/// Chimera eye, the animated eye and TV static. The window is built here
	/// without a display manager, which is all its constructor asks for.
	/// </summary>
	[TestClass]
	public class DisplayConfigTests
	{
		private static readonly (string Text, EIdleScreen Screen)[] Choices =
		[
			("Nothing: the empty screen", EIdleScreen.Off),
			("The Chimera eye, held still", EIdleScreen.Eye),
			("The animated eye: follows the pointer and blinks", EIdleScreen.AnimatedEye),
			("TV static", EIdleScreen.TvStatic),
		];

		private static IEnumerable<Control> All(Control parent)
			=> parent.Controls.Cast<Control>().SelectMany(static c => new[] { c }.Concat(All(c)));

		private static GroupBox IdleGroup(Form form)
			=> All(form).OfType<GroupBox>().Single(g => g.Text == "Idle screen");

		[TestMethod]
		public void TheFourIdleScreensAreOffered()
		{
			foreach (var (text, screen) in Choices)
			{
				using DisplayConfig form = new(new Config { IdleScreen = screen }, null, null);
				form.Show();
				var checkedRadio = All(IdleGroup(form)).OfType<RadioButton>().Single(r => r.Checked);
				Assert.AreEqual(text, checkedRadio.Text, $"{screen} is not the choice the window shows");
			}
		}

		[TestMethod]
		public void ChoosingAnIdleScreenSavesIt()
		{
			using Form owner = new();
			owner.Show();
			foreach (var (text, screen) in Choices)
			{
				Config config = new();
				using DisplayConfig form = new(config, null, null) { Owner = owner };
				form.Show();
				All(form).OfType<RadioButton>().Single(r => r.Text == text).Checked = true;
				All(form).OfType<Button>().Single(b => b.Text == "OK").PerformClick();
				Assert.AreEqual(screen, config.IdleScreen, $"choosing \"{text}\" did not save {screen}");
			}
		}

		/// <summary>A radio button wider than the box it is in is a label that is
		/// cut off, and a box that runs past the tab page is a choice nobody can
		/// see.</summary>
		[TestMethod]
		public void TheFourChoicesFitInTheWindow()
		{
			using DisplayConfig form = new(new Config(), null, null);
			form.Show();
			var group = IdleGroup(form);
			foreach (var radio in All(group).OfType<RadioButton>())
			{
				Assert.IsTrue(radio.Right <= radio.Parent.ClientSize.Width, $"\"{radio.Text}\" is cut off on the right");
			}
			var page = group.Parent.Parent as TabPage ?? throw new AssertFailedException("the idle screen group is not on a tab page");
			Assert.IsTrue(group.Parent.Bottom <= page.ClientSize.Height, $"the idle screen group runs past the {page.Text} tab");
		}

		[TestMethod]
		public void TheWindowInEveryTheme()
		{
			using DisplayConfig form = new(new Config(), null, null);
			// the idle screen lives on Misc, which is the tab worth a picture
			All(form).OfType<TabControl>().Single().SelectedTab = All(form).OfType<TabPage>().Single(p => p.Text == "Misc");
			UiShots.Shoot(form, "display");
		}
	}
}
