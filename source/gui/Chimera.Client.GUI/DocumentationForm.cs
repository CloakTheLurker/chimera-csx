#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Chimera.Client.Common;
using Chimera.Common;

namespace Chimera.Client.GUI
{
	/// <summary>
	/// Help &gt; Open Documentation: the user-facing documents, shipped inside the
	/// window as resources and read here. A list on the left, the chosen document
	/// on the right, rendered by <see cref="MarkdownRtf"/>; a link to another of
	/// these documents opens it, and any other link goes to the browser.
	///
	/// Inside the assembly rather than beside it because the window carries no
	/// files it does not need at run time, and a documentation folder that could
	/// be missing, stale or unreadable would be a Help menu that sometimes helps.
	/// </summary>
	public sealed class DocumentationForm : ThemedForm
	{
		private const string Prefix = "docs/";

		/// <summary>The whole-window document, listed first: the project's own README.</summary>
		private const string Overview = "docs/00-overview.md";

		private sealed record Doc(string Title, string Resource, string Text);

		private readonly List<Doc> _docs = new();
		private readonly ListBox _topics = new();
		private readonly RichTextBox _view = new();
		private readonly SplitContainer _split = new();

		public DocumentationForm() : this(true)
		{
		}

		/// <summary>
		/// A window with no documents of its own when <paramref name="loadShipped"/>
		/// is false, for the standalone viewer (<c>tools/DocumentationViewer</c>),
		/// which fills it from files on disk instead.
		/// </summary>
		public DocumentationForm(bool loadShipped)
		{
			Text = "Documentation";
			ClientSize = new(UIHelper.ScaleX(920), UIHelper.ScaleY(640));
			MinimumSize = new(UIHelper.ScaleX(520), UIHelper.ScaleY(320));
			StartPosition = FormStartPosition.CenterParent;
			ShowIcon = false;

			_topics.Dock = DockStyle.Fill;
			_topics.IntegralHeight = false;

			_view.Dock = DockStyle.Fill;
			_view.ReadOnly = true;
			_view.BorderStyle = BorderStyle.None;
			_view.WordWrap = true;       /* a paragraph is read, not scrolled sideways */
			_view.ScrollBars = RichTextBoxScrollBars.Vertical;
			_view.DetectUrls = true;
			_view.LinkClicked += View_LinkClicked;

			_split.Dock = DockStyle.Fill;
			_split.Orientation = Orientation.Vertical;
			_split.Panel1.Controls.Add(_topics);
			_split.Panel2.Controls.Add(_view);
			Controls.Add(_split);

			if (loadShipped) LoadDocs();
			_topics.SelectedIndexChanged += (_, _) => ShowDoc(_topics.SelectedIndex);
			if (_topics.Items.Count is not 0) _topics.SelectedIndex = 0;
		}

		/// <summary>
		/// The splitter takes its parts here, not in the constructor: a panel's
		/// minimum is measured against the control's width, and before the window is
		/// laid out that width is the SplitContainer's own default, so setting a
		/// minimum from the constructor throws "SplitterDistance must be between
		/// Panel1MinSize and Width - Panel2MinSize" before anything is on screen.
		/// The window has its real width by now.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			var width = _split.Width;
			if (width <= 0) return;
			var listMin = Math.Min(UIHelper.ScaleX(140), Math.Max(1, width / 4));
			var viewMin = Math.Min(UIHelper.ScaleX(320), Math.Max(1, width / 3));
			_split.Panel1MinSize = 0;
			_split.Panel2MinSize = 0;
			var distance = Math.Max(listMin, Math.Min(UIHelper.ScaleX(260), width - viewMin));
			_split.SplitterDistance = distance;
			_split.Panel1MinSize = listMin;
			_split.Panel2MinSize = viewMin;
		}

		/// <summary>The documents this window carries, by resource name: for tests.</summary>
		public IReadOnlyList<string> ShippedDocuments => _docs.Select(static d => d.Resource).ToList();

		/// <summary>The titles the list shows, in order: for tests.</summary>
		public IReadOnlyList<string> Topics => _docs.Select(static d => d.Title).ToList();

		/// <summary>Shows the document at <paramref name="index"/>, as choosing it does: for tests.</summary>
		public void ShowTopic(int index) => ShowDoc(index);

		/// <summary>
		/// Adds a document this window did not carry, and lists it like the rest:
		/// what the standalone viewer uses to show a file from disk. The file name
		/// is the key links between documents resolve against.
		/// </summary>
		public void LoadMarkdown(string fileName, string markdown)
		{
			var title = MarkdownRtf.TitleOf(markdown) ?? Path.GetFileNameWithoutExtension(fileName);
			_docs.Add(new Doc(title, fileName, markdown));
			_topics.Items.Add(title);
			/* the first document a viewer adds is the one to show */
			if (_topics.SelectedIndex < 0) _topics.SelectedIndex = 0;
		}

		/// <summary>The rendered document on screen, for tests.</summary>
		public RichTextBox Viewer => _view;

		private void LoadDocs()
		{
			var assembly = typeof(DocumentationForm).Assembly;
			var names = assembly.GetManifestResourceNames()
				.Where(static n => n.StartsWith(Prefix, StringComparison.Ordinal)
					&& n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				.OrderBy(static n => n == Overview ? "" : n, StringComparer.Ordinal)
				.ToList();
			foreach (var name in names)
			{
				using var stream = assembly.GetManifestResourceStream(name);
				if (stream is null) continue;
				using var reader = new StreamReader(stream);
				var text = reader.ReadToEnd();
				/* the overview is the project's README, whose title is its badges: the
				 * window names it, and every other document names itself */
				var title = name == Overview
					? "Chimera CSX"
					: MarkdownRtf.TitleOf(text) ?? Path.GetFileNameWithoutExtension(name);
				_docs.Add(new Doc(title, name, text));
			}
			_topics.BeginUpdate();
			foreach (var doc in _docs) _topics.Items.Add(doc.Title);
			_topics.EndUpdate();
		}

		private void ShowDoc(int index)
		{
			if (index < 0 || index >= _docs.Count) return;
			var doc = _docs[index];
			_view.Rtf = MarkdownRtf.ToRtf(doc.Text, ReaderStyle());
			_view.SelectionStart = 0;
			_view.SelectionLength = 0;
			_view.ScrollToCaret();
			Text = $"Documentation - {doc.Title}";
		}

		/// <summary>
		/// A theme change repaints the document as well: its colours live inside the
		/// RTF, which the theme walk cannot reach, so otherwise the text would keep
		/// the colour of the theme the window just left - dark grey over a window
		/// that has gone dark.
		/// </summary>
		protected override void ApplyTheme(Theme theme)
		{
			base.ApplyTheme(theme);
			if (_docs.Count is not 0 && _topics.SelectedIndex >= 0) ShowDoc(_topics.SelectedIndex);
		}

		/// <summary>
		/// Whether a hyperlink field survives in this window's reader. Windows'
		/// RichTextBox understands one and answers a click on it; Mono's drops the
		/// field and the text inside it, which would leave a gap where a link was.
		/// Where a field does not survive, the renderer writes the label and the
		/// address, and the control's own URL detection makes the address clickable.
		/// </summary>
		private static readonly bool LinkFieldsWork = Type.GetType("Mono.Runtime") is null;

		/// <summary>
		/// The reader's colours and size from the theme in force: the RTF carries a
		/// colour table of its own, and a fixed dark grey over a dark window is text
		/// nobody can see. 12 pt, where the reader used to draw 10.
		/// </summary>
		private static MarkdownRtfStyle ReaderStyle() => new()
		{
			BaseFontSize = 24,
			Body = ThemeEngine.Color(ThemeColorRole.InputText),
			Link = ThemeEngine.Color(ThemeColorRole.LinkText),
			Muted = ThemeEngine.Color(ThemeColorRole.MutedText),
			HyperlinkFields = LinkFieldsWork,
		};

		private void View_LinkClicked(object? sender, LinkClickedEventArgs e)
		{
			var target = (e.LinkText ?? "").Trim();
			if (target.Length is 0) return;
			/* another of these documents: its file name is the last part of the link,
			 * whatever folder the document wrote it from */
			var name = target.Replace('\\', '/');
			var slash = name.LastIndexOf('/');
			if (slash >= 0) name = name[(slash + 1)..];
			var doc = _docs.FirstOrDefault(d =>
				Path.GetFileName(d.Resource).Equals(name, StringComparison.OrdinalIgnoreCase));
			if (doc is not null)
			{
				_topics.SelectedIndex = _docs.IndexOf(doc);
				return;
			}
			Util.OpenUrlExternal(target);
		}
	}
}
