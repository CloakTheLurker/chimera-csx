#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Chimera.Client.Common;
using Chimera.Client.GUI;

namespace Chimera.Tests.Client.GUI
{
	/// <summary>
	/// Help &gt; Open Documentation: the documents are inside the window, not beside
	/// it, so the menu cannot point at a folder that is missing or stale. Every one
	/// of them has a title to list it under, and every one of them renders.
	/// </summary>
	[TestClass]
	public class DocumentationTests
	{
		[TestMethod]
		public void TheDocumentsAreInsideTheWindowAndEveryOneRenders()
		{
			using DocumentationForm form = new();
			var shipped = form.ShippedDocuments;
			CollectionAssert.Contains(shipped.ToList(), "docs/00-overview.md");
			foreach (var expected in new[] { "docs/project.md", "docs/core-manager.md", "docs/game-cores.md", "docs/state-manager.md" })
			{
				CollectionAssert.Contains(shipped.ToList(), expected);
			}

			Assert.AreEqual(shipped.Count, form.Topics.Count);
			Assert.IsTrue(form.Topics.All(static t => !string.IsNullOrWhiteSpace(t)), "every document is listed under a title");
			Assert.AreEqual("Chimera CSX", form.Topics[0], "the window's own overview comes first");

			for (var i = 0; i < shipped.Count; i++)
			{
				form.ShowTopic(i);
				var rtf = form.Viewer.Rtf;
				Assert.IsFalse(string.IsNullOrWhiteSpace(rtf), $"{shipped[i]} renders to nothing");
				StringAssert.StartsWith(rtf, "{\\rtf1", $"{shipped[i]} does not render to RTF");
			}
		}

		/// <summary>
		/// The window opens: its splitter takes its parts only once the window has a
		/// width, and taking them too early is the exception a person saw rather than
		/// a cold window. What the test can hold is that opening it works and the
		/// first document is drawn.
		/// </summary>
		[TestMethod]
		public void TheWindowOpensWithADocumentOnScreen()
		{
			using DocumentationForm form = new();
			form.Show();
			form.Refresh();
			Assert.IsTrue(form.Visible);
			Assert.IsFalse(string.IsNullOrWhiteSpace(form.Viewer.Rtf), "the first document is on screen");
			form.Close();
		}

		/// <summary>
		/// The reader wraps: a paragraph is read, not scrolled sideways. A table
		/// and a code block used to keep their lines and force a horizontal bar on
		/// every document that had one.
		/// </summary>
		[TestMethod]
		public void TheReaderWrapsItsText()
		{
			using DocumentationForm form = new();
			Assert.IsTrue(form.Viewer.WordWrap, "the reader must wrap a paragraph rather than scroll it sideways");
			Assert.AreEqual(RichTextBoxScrollBars.Vertical, form.Viewer.ScrollBars);
		}

		/// <summary>
		/// The standalone viewer (tools/DocumentationViewer) puts a file from disk
		/// into this window: it is listed under its heading and draws like one of
		/// the shipped documents, so what the viewer shows is what Help shows.
		/// </summary>
		[TestMethod]
		public void ADocumentFromDiskIsListedAndDrawn()
		{
			using DocumentationForm form = new(loadShipped: false);
			var before = form.Topics.Count;
			Assert.AreEqual(0, before, "the viewer starts with no documents of its own");
			form.LoadMarkdown("sample.md", "# A sample\n\nSome text with a [link](https://example.com).\n");
			Assert.AreEqual(before + 1, form.Topics.Count);
			Assert.AreEqual("A sample", form.Topics[before]);
			form.ShowTopic(before);
			Assert.IsFalse(string.IsNullOrWhiteSpace(form.Viewer.Text), "the document from disk is drawn");
			// the words after a blank line must all be there: a paragraph mark
			// without its delimiter used to swallow the next word
			StringAssert.Contains(form.Viewer.Text, "Some text with a link");
		}

		[TestMethod]
		public void EveryDocumentHasAHeadingToNameIt()
		{
			var assembly = typeof(DocumentationForm).Assembly;
			var names = assembly.GetManifestResourceNames()
				.Where(static n => n.StartsWith("docs/", StringComparison.Ordinal) && n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
				.ToList();
			Assert.IsTrue(names.Count > 10, $"only {names.Count} documents were embedded");
			foreach (var name in names)
			{
				// the overview is the README, whose title is its badges; the window names it
				if (name == "docs/00-overview.md") continue;
				using var stream = assembly.GetManifestResourceStream(name);
				Assert.IsNotNull(stream, $"{name} is listed but not readable");
				using var reader = new StreamReader(stream!);
				var text = reader.ReadToEnd();
				Assert.IsFalse(string.IsNullOrWhiteSpace(MarkdownRtf.TitleOf(text)), $"{name} has no level-one heading");
			}
		}
	}
}
