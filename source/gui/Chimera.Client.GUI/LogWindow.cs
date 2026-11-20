using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Chimera.Common;
using Chimera.Emulation.Common;
using Chimera.Client.Common;
using Chimera.Common.StringExtensions;

// todo - perks - pause, copy to clipboard, backlog length limiting

namespace Chimera.Client.GUI
{
	public partial class LogWindow : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.CommandWindow;

		/// <summary>Whether the log is kept while this window is closed. Its place
		/// is in the window's own Settings menu, above the window options the tool
		/// manager adds there.</summary>
		private readonly ToolStripMenuItem _runInBackground = new()
		{
			Text = "Run in &Background",
			ToolTipText = "Keep the log while this window is closed",
		};

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private string _windowTitle = "Log Window";

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => "Log Window";

		public LogWindow()
		{
			InitializeComponent();
			Icon = ToolIcon;
			AddToGameDbBtn.Image = Properties.Resources.Add;
			var settings = new ToolStripMenuItem("&Settings");
			settings.DropDownItems.Add(_runInBackground);
			MenuStrip.Items.Add(settings);
			_runInBackground.Click += RunInBackground_Click;
			Closing += (o, e) =>
			{
				if (Tools is null) return;
				Tools.Log.Changed -= OnLogChanged;
				Tools.Log.ViewClosed();
			};
			ListView_ClientSizeChanged(null, EventArgs.Empty);
		}

		private LogBuffer Log => Tools?.Log;

		/// <summary>A line this window did not get through the console: the on-screen
		/// messages, which are as much a record as the console's own.</summary>
		public void Append(string message) => Log?.Append(message);

		public void ShowReport(string title, string report)
		{
			Log?.ShowReport(report);
			_windowTitle = title;
			UpdateWindowTitle();
			btnClear.Visible = false;
		}

		private void RunInBackground_Click(object sender, EventArgs e)
		{
			if (Tools is null) return;
			var on = !_runInBackground.Checked;
			_runInBackground.Checked = on;
			Config.LogWindowBackground = on;
			Tools.Log.SetBackground(on);
		}

		// Any thread writes the console, and a thread can still be writing while this
		// window closes, or before it has a handle. Posting an update then throws on the
		// writing thread, and an exception there ends the whole process; the line is kept
		// regardless and shows with the next update.
		private void OnLogChanged()
		{
			if (!IsHandleCreated || IsDisposed) return;
			try
			{
				BeginInvoke(doUpdateListSize);
			}
			catch (InvalidOperationException)
			{
				// the window went away between the check and the post (ObjectDisposedException is one of these)
			}
		}

		private void doUpdateListSize()
		{
			if (IsDisposed) return;
			var count = Log?.Count ?? 0;
			virtualListView1.VirtualListSize = count;
			if (count > 0) virtualListView1.EnsureVisible(count - 1);
		}

		private void BtnClear_Click(object sender, EventArgs e)
		{
			Log?.Clear();
			virtualListView1.SelectedIndices.Clear();
			doUpdateListSize();
		}

		private void BtnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LogWindow_Load(object sender, EventArgs e)
		{
			HideShowGameDbButton();
			if (Tools is null) return;
			_runInBackground.Checked = Config.LogWindowBackground;
			Tools.Log.Changed += OnLogChanged;
			Tools.Log.ViewOpened();
			doUpdateListSize();
		}

		private void ListView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var log = Log;
			e.Item = new ListViewItem(log is not null && e.ItemIndex < log.Count ? log[e.ItemIndex] : string.Empty);
		}

		private void ListView_ClientSizeChanged(object sender, EventArgs e)
		{
			virtualListView1.Columns[0].Width = virtualListView1.ClientSize.Width;
		}

		private void ListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsCtrl(Keys.C))
			{
				ButtonCopy_Click(null, EventArgs.Empty);
			}
		}

		private void ButtonCopy_Click(object sender, EventArgs e)
		{
			var log = Log;
			if (log is null) return;
			string s;
			if (virtualListView1.SelectedIndices.Count > 1)
			{
				StringBuilder sb = new();
				foreach (int i in virtualListView1.SelectedIndices) sb.AppendLine(log[i]);
				s = sb.ToString();
			}
			else if (virtualListView1.SelectedIndices.Count is 1)
			{
				s = log[virtualListView1.SelectedIndices[0]]
					.RemovePrefix(SHA1Checksum.PREFIX + ":")
					.RemovePrefix(MD5Checksum.PREFIX + ":");
			}
			else
			{
				return;
			}
			s = s.Trim();
			if (!string.IsNullOrWhiteSpace(s)) Clipboard.SetText(s);
		}

		private void ButtonCopyAll_Click(object sender, EventArgs e)
		{
			var log = Log;
			if (log is null) return;
			var sb = new StringBuilder();
			for (var i = 0; i < log.Count; i++) sb.AppendLine(log[i]);
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString());
		}

		private void HideShowGameDbButton()
		{
			AddToGameDbBtn.Visible = false; // Chimera has no game database
		}
	}
}
