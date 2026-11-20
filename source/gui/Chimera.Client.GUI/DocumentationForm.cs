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

		public DocumentationForm()
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
			_view.WordWrap = false;      /* a table and a code block keep their lines */
			_view.ScrollBars = RichTextBoxScrollBars.Both;
			_view.DetectUrls = true;
			_view.LinkClicked += View_LinkClicked;

			_split.Dock = DockStyle.Fill;
			_split.Orientation = Orientation.Vertical;
			_split.Panel1.Controls.Add(_topics);
			_split.Panel2.Controls.Add(_view);
			_split.Panel1MinSize = UIHelper.ScaleX(140);
			_split.Panel2MinSize = UIHelper.ScaleX(320);
			Controls.Add(_split);

			LoadDocs();
			_split.SplitterDistance = UIHelper.ScaleX(260);
			_topics.SelectedIndexChanged += (_, _) => ShowDoc(_topics.SelectedIndex);
			if (_topics.Items.Count is not 0) _topics.SelectedIndex = 0;
		}

		/// <summary>The documents this window carries, by resource name: for tests.</summary>
		public IReadOnlyList<string> ShippedDocuments => _docs.Select(static d => d.Resource).ToList();

		/// <summary>The titles the list shows, in order: for tests.</summary>
		public IReadOnlyList<string> Topics => _docs.Select(static d => d.Title).ToList();

		/// <summary>Shows the document at <paramref name="index"/>, as choosing it does: for tests.</summary>
		public void ShowTopic(int index) => ShowDoc(index);

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
			_view.Rtf = MarkdownRtf.ToRtf(doc.Text);
			_view.SelectionStart = 0;
			_view.SelectionLength = 0;
			_view.ScrollToCaret();
			Text = $"Documentation - {doc.Title}";
		}

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
