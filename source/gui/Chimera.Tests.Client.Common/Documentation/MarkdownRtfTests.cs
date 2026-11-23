#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
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
			StringAssert.Contains(rtf, "\\b\\fs40 The title");
			StringAssert.Contains(rtf, "\\b\\fs32 A section");
			StringAssert.Contains(rtf, "A paragraph.");
			StringAssert.Contains(rtf, "\\b0 \\par \\pard\\f0\\fs24\\cf1 \\par ",
				"a heading ends its paragraph and leaves an empty one in the body's face");
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

		/// <summary>The reader is drawn in the colours and size its window gives it:
		/// a fixed colour in the document was invisible over the dark theme.</summary>
		[TestMethod]
		public void TheDocumentIsDrawnInTheColoursAndSizeItIsGiven()
		{
			var rtf = MarkdownRtf.ToRtf("Body text", new MarkdownRtfStyle
			{
				BaseFontSize = 28,
				Body = Color.FromArgb(1, 2, 3),
				Link = Color.FromArgb(4, 5, 6),
				Muted = Color.FromArgb(7, 8, 9),
			});
			StringAssert.Contains(rtf, "\\red1\\green2\\blue3", "the body colour is the one given");
			StringAssert.Contains(rtf, "\\red4\\green5\\blue6", "the link colour is the one given");
			StringAssert.Contains(rtf, "\\red7\\green8\\blue9", "the muted colour is the one given");
			StringAssert.Contains(rtf, "\\fs28", "the body is the size given");
		}

		/// <summary>The reader cannot draw a picture, and its label would be a link
		/// to a file nobody can open: the whole thing goes.</summary>
		[TestMethod]
		public void APictureIsDroppedRatherThanShownAsABrokenLink()
		{
			var rtf = MarkdownRtf.ToRtf("before ![the badge](docs/icon.svg) after");
			StringAssert.Contains(rtf, "before ");
			StringAssert.Contains(rtf, " after");
			Assert.IsFalse(rtf.Contains("the badge"), "the picture's label is still there");
			Assert.IsFalse(rtf.Contains("icon.svg"), "the picture's file is still there");
			Assert.IsFalse(rtf.Contains("HYPERLINK"), "the picture became a link");
		}

		/// <summary>A table row is text in a fixed face; a link in one of its cells
		/// is still a link, underlined and clickable, not the raw Markdown.</summary>
		[TestMethod]
		public void ALinkInATableIsStillALink()
		{
			var rtf = MarkdownRtf.ToRtf("| a | [the doc](docs/project.md) |");
			StringAssert.Contains(rtf, "HYPERLINK \"docs/project.md\"");
			StringAssert.Contains(rtf, "\\ul\\cf2 the doc");
			Assert.IsFalse(rtf.Contains("[the doc](docs/project.md)"), "the raw Markdown was printed");
		}

		/// <summary>Mono's reader throws a hyperlink field away, text and all, so a
		/// link written as one is a gap in the sentence there. A reader without
		/// fields keeps the label and the address, which its URL detection can make
		/// clickable.</summary>
		[TestMethod]
		public void AReaderWithoutFieldsStillShowsTheLink()
		{
			var rtf = MarkdownRtf.ToRtf("see [the doc](docs/project.md) now",
				new MarkdownRtfStyle { HyperlinkFields = false });
			StringAssert.Contains(rtf, "the doc (docs/project.md)");
			StringAssert.Contains(rtf, "see ");
			StringAssert.Contains(rtf, " now");
			Assert.IsFalse(rtf.Contains("HYPERLINK"), "a field would be dropped along with its text");
		}

		/// <summary>A page of HTML in the README is read as its text: the tags go,
		/// and so does a line that held only the spaces between two pictures.</summary>
		[TestMethod]
		public void ALineOfHtmlIsItsTextAndNothingElse()
		{
			var rtf = MarkdownRtf.ToRtf("<p align=\"center\">Chimera &amp; friends</p>\n\n\t&nbsp;&nbsp;\n\nAfter.\n");
			StringAssert.Contains(rtf, "Chimera & friends");
			Assert.IsFalse(rtf.Contains("nbsp"), "an entity was printed as it is written");
			Assert.IsFalse(rtf.Contains("<p"), "an HTML tag was printed");
		}

		/// <summary>A table is rows a reader reads as a table: a blank line between
		/// each row would pull the columns apart into separate blocks.</summary>
		[TestMethod]
		public void TheRowsOfATableStayTogether()
		{
			var rtf = MarkdownRtf.ToRtf("| a | b |\n|---|---|\n| c | d |\n");
			var from = rtf.IndexOf("a | b", StringComparison.Ordinal);
			var to = rtf.IndexOf("c | d", StringComparison.Ordinal);
			Assert.IsTrue(from >= 0 && to > from, "both rows are drawn");
			Assert.IsFalse(rtf.Substring(from, to - from).Contains("\\par \\par ", StringComparison.Ordinal),
				"a blank line was left between the rows");
		}

		/// <summary>
		/// RTF reads a control word up to the first character that is not a letter,
		/// so "\par" written against the text after it is the word "parSome", which
		/// nothing defines, and the text is gone with it: that is the fault that
		/// lost the first word of a paragraph. Every control word here has to be one
		/// the reader knows, which means each ends before the text it precedes.
		/// </summary>
		[TestMethod]
		public void EveryControlWordIsOneTheReaderKnows()
		{
			var rtf = MarkdownRtf.ToRtf(
				"# Title\n\nOne paragraph with **bold**, `code` and [a link](docs/project.md) in it.\n\n" +
				"- a bullet\n\n1. a number\n\n> a quote\n\n| a | b |\n\n---\n\n```\ncode\n```\n");
			var known = new HashSet<string>(StringComparer.Ordinal)
			{
				"rtf", "ansi", "ansicpg", "uc", "deff", "fonttbl", "f", "fswiss", "fmodern",
				"colortbl", "red", "green", "blue", "pard", "sb", "sa", "b", "fs", "cf", "par",
				"li", "fi", "ri", "i", "field", "fldinst", "fldrslt", "ul", "brdrb", "brdrs", "brdrw",
				"brsp", "line", "tab", "u",
			};
			for (var at = 0; at < rtf.Length; at++)
			{
				if (rtf[at] != '\\') continue;
				if (at + 1 < rtf.Length && rtf[at + 1] == '\\') { at++; continue; }  // an escaped backslash
				var end = at + 1;
				while (end < rtf.Length && char.IsLetter(rtf[end])) end++;
				if (end == at + 1) continue;                                        // \{ \} \* and numbers
				var word = rtf[(at + 1)..end];
				Assert.IsTrue(known.Contains(word), $"'{word}' is not a control word; it has run into the text after it");
				at = end - 1;
			}
		}

		[TestMethod]
		public void TheLinksAdocumentNamesAreFound()
		{
			var links = MarkdownRtf.LinksToDocs("see [a](project.md) and [b](../README.md) and [c](https://example.invalid)\n");
			CollectionAssert.AreEqual(new[] { "project.md", "../README.md" }, links.ToArray());
		}
	}
}
