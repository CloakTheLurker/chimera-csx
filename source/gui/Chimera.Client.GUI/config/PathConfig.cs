using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Chimera.Client.Common;
using Chimera.Common;
using Chimera.Emulation.Common;

namespace Chimera.Client.GUI
{
	public sealed partial class PathConfig : ThemedForm, IDialogParent
	{
		private readonly PathEntryCollection _pathEntries;

		private readonly Action<string> _setMovieBackupPath;

		private static AutoCompleteStringCollection AutoCompleteOptions => new AutoCompleteStringCollection
		{
			"%recent%",
			"%exe%",
			"%rom%",
		};

		public IDialogController DialogController { get; }

		public PathConfig(
			IDialogController dialogController,
			PathEntryCollection pathEntries,
			string sysID,
			Action<string> setMovieBackupPath)
		{
			_pathEntries = pathEntries;
			_setMovieBackupPath = setMovieBackupPath;
			DialogController = dialogController;
			InitializeComponent();
			SpecialCommandsBtn.Image = Properties.Resources.Help;
		}

		private void LoadSettings()
		{
			RecentForROMs.Checked = _pathEntries.UseRecentForRoms;

			DoTabs(_pathEntries.Paths);
			DoRomToggle();
		}

		private void DoTabs(IReadOnlyList<PathEntry> pathCollection)
		{
			int x = UIHelper.ScaleX(6);
			int textBoxWidth = UIHelper.ScaleX(70);
			int padding = UIHelper.ScaleX(5);
			int buttonWidth = UIHelper.ScaleX(26);
			int buttonHeight = UIHelper.ScaleY(23);
			int buttonOffsetY = -1; // To align the top with the TextBox I guess? Always 1 pixel regardless of scaling.
			int widgetOffset = UIHelper.ScaleX(85);
			int rowHeight = UIHelper.ScaleY(30);

			void PopulateTabPage(Control t, string system)
			{
				var paths = pathCollection
					.Where(p => p.System == system)
					.OrderBy(p => p.Ordinal)
					.ThenBy(p => p.Type);

				var y = UIHelper.ScaleY(14);
				foreach (var path in paths)
				{
					var box = new TextBox
					{
						Text = path.Path,
						Location = new Point(x, y),
						Width = textBoxWidth,
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						MinimumSize = new Size(UIHelper.ScaleX(26), UIHelper.ScaleY(23)),
						AutoCompleteMode = AutoCompleteMode.SuggestAppend,
						AutoCompleteCustomSource = AutoCompleteOptions,
						AutoCompleteSource = AutoCompleteSource.CustomSource,
					};

					var btn = new Button
					{
						Text = "",
						Image = Properties.Resources.OpenFile,
						Location = new Point(widgetOffset, y + buttonOffsetY),
						Size = new Size(buttonWidth, buttonHeight),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					var tempBox = box;
					var tempPath = path.Type;
					var tempSystem = path.System;
					btn.Click += (sender, args) => BrowseFolder(tempBox, tempPath, tempSystem);

					var label = new Label
					{
						Text = path.Type,
						Location = new Point(widgetOffset + buttonWidth + padding, y + UIHelper.ScaleY(4)),
						Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
						Name = path.Type,
						Anchor = AnchorStyles.Top | AnchorStyles.Right,
					};

					t.Controls.Add(label);
					t.Controls.Add(btn);
					t.Controls.Add(box);

					y += rowHeight;
				}
			}

			/* The dialog edits the Global paths. The "by System" page went (user,
			 * 2026-11-19): a project names its files, the cores ask for firmware and
			 * their own data through their packages, and the per-system entries had
			 * become a place to be wrong in. Their values stay in the collection and
			 * still fall back as they did. */
			tcMain.Visible = false;
			tpGlobal.Controls.Clear();
			tpGlobal.Name = PathEntryCollection.GLOBAL; // required for SaveSettings
			var hack = tpGlobal.Size.Width - UIHelper.ScaleX(220); // whyyyyyyyyyy
			textBoxWidth += hack;
			widgetOffset += hack;
			Size hack1 = new(17, 0); // also whyyyyyyyyyy
			PopulateTabPage(tpGlobal, PathEntryCollection.GLOBAL);
			tpGlobal.Controls[tpGlobal.Controls.Count - 1].Size -= hack1; // TextBox
			tpGlobal.Controls[tpGlobal.Controls.Count - 2].Location -= hack1; // Button
			tcMain.Visible = true;
		}

		private void BrowseFolder(TextBox box, string name, string system)
		{
			// Ugly hack, we don't want to pass in the system in for system base and global paths
			if (system is not null && (name is "Base" || system is "Global" || system == PathEntryCollection.GLOBAL))
			{
				BrowseFolder(box, name, system: null);
				return;
			}
			var selectedPath = this.ShowFolderSelectDialog(
				initDir: _pathEntries.AbsolutePathFor(box.Text, system),
				subtitle: $"Set the directory for {name}");
			if (selectedPath is not null)
			{
				box.Text = _pathEntries.TryMakeRelative(selectedPath, system);
			}
		}

		private void SaveSettings()
		{
			_pathEntries.UseRecentForRoms = RecentForROMs.Checked;

			foreach (var t in AllPathControls.OfType<TextBox>())
			{
				var pathEntry = _pathEntries.Paths.First(p => p.System == t.Parent.Name && p.Type == t.Name);
				pathEntry.Path = t.Text;
			}

			_setMovieBackupPath(_pathEntries.MovieBackupsAbsolutePath());
		}

		private void DoRomToggle()
		{
			foreach (var control in AllPathControls.Where(c => c.Name == "ROM"))
			{
				control.Enabled = !RecentForROMs.Checked;
			}
		}

		private IEnumerable<Control> AllPathControls => tpGlobal.Controls();

		private void NewPathConfig_Load(object sender, EventArgs e)
		{
			LoadSettings();
		}

		private void RecentForRoms_CheckedChanged(object sender, EventArgs e)
		{
			DoRomToggle();
		}

		private void SpecialCommandsBtn_Click(object sender, EventArgs e)
		{
			new PathInfo().Show();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			SaveSettings();
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
			=> DoTabs(PathEntryCollection.Defaults.Value);

		private void Ok_Click(object sender, EventArgs e)
		{
			SaveSettings();

			_pathEntries.RefreshTempPath();
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}

