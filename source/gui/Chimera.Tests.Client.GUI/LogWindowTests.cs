using System.Linq;
using System.Windows.Forms;

using Chimera.Client.GUI;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// The Log Window's own Settings menu. The switch is at the top of it, above
	/// the window options the tool manager adds when the manager opens the
	/// window; here the window is built on its own, which is enough to see that
	/// the menu and its item exist.
	/// </summary>
	[TestClass]
	public class LogWindowTests
	{
		[TestMethod]
		public void TheWindowOffersTheBackgroundSwitch()
		{
			using LogWindow window = new();
			window.Show();
			var settings = window.MainMenuStrip.Items.OfType<ToolStripMenuItem>().Single(i => i.Text.Contains("Settings"));
			var runInBackground = settings.DropDownItems.OfType<ToolStripMenuItem>()
				.SingleOrDefault(i => i.Text == "Run in &Background");
			Assert.IsNotNull(runInBackground, "the Log Window has no Run in Background switch");
			Assert.AreSame(runInBackground, settings.DropDownItems[0], "the switch is not at the top of the menu");
		}
	}
}
