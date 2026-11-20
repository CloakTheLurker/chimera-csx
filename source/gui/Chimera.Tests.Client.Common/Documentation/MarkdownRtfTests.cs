#nullable enable

using System;
using System.Linq;

using Chimera.Client.Common;

namespace Chimera.Tests.Client.Common.Documentation
{
	/// <summary>
	/// The Markdown the window's own reader shows (Help &gt; Open Documentation):
	/// the shapes these documents are written in reach the RTF as the reader has to
	/// see them - a heading as a heading, a code block as code and not a heading, a
	/// link as a hyperlink field - and anything RTF treats specially is escaped so a
	/// document cannot break the file it is drawn from.
	/// </summary>
	[TestClass]
	public class MarkdownRtfTests
	{
		[TestMethod]
		public void AHeadingIsBigAndBoldAndACommentIsNotAHeading()
		{
			var rtf = MarkdownRtf.ToRtf("# The title\n\nA paragraph.\n\n## A section\n");
			StringAssert.Contains(rtf, "\\b\\fs32 The title");
			StringAssert.Contains(rtf, "\\b\\fs26 A section");
			StringAssert.Contains(rtf, "A paragraph.");
			StringAssert.StartsWith(rtf, "{\\rtf1");
			StringAssert.EndsWith(rtf, "}");
			Assert.AreEqual("The title", MarkdownRtf.TitleOf("# The title\n\nA paragraph."));
			Assert.IsNull(MarkdownRtf.TitleOf("no heading here"));
		}

		[TestMethod]
		public void FencedCodeIsCodeAndKeepsItsCharacters()
		{
			var rtf = MarkdownRtf.ToRtf("```\n# not a heading\nif (a) { b(); }\n```\n");
			StringAssert.Contains(rtf, "\\f1");
			StringAssert.Contains(rtf, "# not a heading", "a code block keeps its own hashes");
			StringAssert.Contains(rtf, "if (a) \\{ b(); \\}", "and its braces are RTF's text, not its commands");
			Assert.IsNull(MarkdownRtf.TitleOf("```\n# not a heading\n```"), "a fenced heading is not the document's title");
		}

		[TestMethod]
		public void EmphasisAndInlineCodeAreRuns()
		{
			var rtf = MarkdownRtf.ToRtf("a **bold** word, an *italic* one and `code()` too\n");
			StringAssert.Contains(rtf, "\\b bold\\b0");
			StringAssert.Contains(rtf, "\\i italic\\i0");
			StringAssert.Contains(rtf, "\\f1 code()\\f0");
		}

		[TestMethod]
		public void ALinkIsAFieldTheReaderCanFollow()
		{
			var rtf = MarkdownRtf.ToRtf("See [the project](project.md) and [the site](https://example.invalid/a).\n");
			StringAssert.Contains(rtf, "{\\field{\\*\\fldinst{HYPERLINK \"project.md\"}}");
			StringAssert.Contains(rtf, "HYPERLINK \"https://example.invalid/a\"");
			StringAssert.Contains(rtf, "\\ul\\cf2 the project");
		}

		[TestMethod]
		public void TextRtfWouldEatIsWrittenAsText()
		{
			var rtf = MarkdownRtf.ToRtf("a {brace} and a \\ and a dash \u2014 and an \u00e9\n");
			StringAssert.Contains(rtf, "a \\{brace\\} and a \\\\ and");
			StringAssert.Contains(rtf, "\\u8212?", "a character above ASCII is written as its code point");
			StringAssert.Contains(rtf, "\\u233?");
		}

		[TestMethod]
		public void ListsQuotesAndTablesKeepTheirShape()
		{
			var rtf = MarkdownRtf.ToRtf("- one\n- two\n\n> quoted\n\n| a | b |\n| --- | --- |\n| 1 | 2 |\n");
			StringAssert.Contains(rtf, "\\u8226? one");
			StringAssert.Contains(rtf, "\\u8226? two");
			StringAssert.Contains(rtf, "\\i quoted\\i0");
			StringAssert.Contains(rtf, "a | b");
			StringAssert.Contains(rtf, "1 | 2");
			Assert.IsFalse(rtf.Contains("--- | ---"), "the row of dashes under a header says nothing to read");
		}

		[TestMethod]
		public void TheLinksAdocumentNamesAreFound()
		{
			var links = MarkdownRtf.LinksToDocs("see [a](project.md) and [b](../README.md) and [c](https://example.invalid)\n");
			CollectionAssert.AreEqual(new[] { "project.md", "../README.md" }, links.ToArray());
		}
	}
}
