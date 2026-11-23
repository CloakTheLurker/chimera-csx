#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Chimera.Client.Common
{
	/// <summary>
	/// How a document is drawn. The reader supplies these from the current theme,
	/// so text is never painted in a colour the window is not using: the RTF carries
	/// its own colour table, and a fixed dark grey over a dark window is text nobody
	/// can see.
	/// </summary>
	public sealed record MarkdownRtfStyle
	{
		/// <summary>Body text size in RTF half-points: 24 is 12 pt.</summary>
		public int BaseFontSize { get; init; } = 24;

		public Color Body { get; init; } = Color.FromArgb(32, 32, 32);

		public Color Link { get; init; } = Color.FromArgb(70, 110, 175);

		/// <summary>Code, tables and other secondary text.</summary>
		public Color Muted { get; init; } = Color.FromArgb(110, 110, 110);

		/// <summary>
		/// Whether the reader understands a hyperlink field, which is what makes a
		/// link clickable in Windows' RichTextBox. Mono's drops the field and the
		/// text inside it, so a document read there would show a gap where the link
		/// was; when this is false a link is written as its label followed by its
		/// address, and the control's own URL detection makes the address clickable.
		/// </summary>
		public bool HyperlinkFields { get; init; } = true;
	}

	/// <summary>
	/// A Markdown document as RTF, for the window's own reader (Help &gt; Open
	/// Documentation). It knows the shapes these documents are written in -
	/// headings, paragraphs, lists, quotes, fenced code, tables, inline code,
	/// bold and italic, links - and anything else is written as the text it is
	/// rather than dropped, so a document that outgrows it still reads.
	///
	/// RTF rather than HTML because the reader is a RichTextBox: it is the one
	/// control that shows styled text on Windows and under Mono alike, and its
	/// hyperlink fields answer clicks.
	/// </summary>
	public static class MarkdownRtf
	{
		/// <summary>
		/// The document's first level-one heading, without the hashes, or null when
		/// it has none: the name the reader lists it under.
		/// </summary>
		public static string? TitleOf(string markdown)
		{
			var inFence = false;
			foreach (var raw in Lines(markdown))
			{
				var line = raw.Trim();
				if (line.StartsWith("```", StringComparison.Ordinal))
				{
					inFence = !inFence;
					continue;
				}
				if (inFence) continue;
				if (line.StartsWith("# ", StringComparison.Ordinal) && line.Length > 2) return line[2..].Trim();
			}
			return null;
		}

		/// <summary>The same document as RTF, ready for a RichTextBox.</summary>
		public static string ToRtf(string markdown, MarkdownRtfStyle? style = null)
		{
			style ??= new MarkdownRtfStyle();
			var baseSize = Math.Max(12, style.BaseFontSize);
			var codeSize = Math.Max(10, baseSize * 9 / 10);
			var fields = style.HyperlinkFields;
			var body = new StringBuilder();
			var inFence = false;
			/* what the text in hand ends with, followed as the loop goes: whether a
			 * paragraph has ended, and whether the one it is in is empty */
			var paragraphEnded = true;
			var blankLine = false;

			/* every paragraph mark carries a space after it because RTF reads a
			 * control word up to the first character that is not a letter: a mark
			 * written straight against the text after it is a word of its own that
			 * nothing defines, and it takes that text with it */
			void EndParagraph()
			{
				if (paragraphEnded) return;
				body.Append("\\par ");
				paragraphEnded = true;
				blankLine = false;
			}

			/* the empty line between two paragraphs, once */
			void BlankLine()
			{
				if (body.Length is 0 || blankLine) return;
				EndParagraph();
				body.Append("\\par ");
				blankLine = true;
			}

			/* the body's own face, for what follows a block that set its own - a
			 * heading's spacing, a quote's or a bullet's indent, a table row's face */
			void BodyFace() => body.Append("\\pard\\f0\\fs").Append(baseSize).Append("\\cf1 ");

			foreach (var raw in Lines(markdown))
			{
				var line = raw.TrimEnd();

				if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
				{
					inFence = !inFence;
					body.Append(inFence
						? "\\pard\\f1\\fs" + codeSize + "\\cf3 "
						: "\\pard\\f0\\fs" + baseSize + "\\cf1 ");
					paragraphEnded = false;
					blankLine = false;
					continue;
				}

				if (inFence)
				{
					body.Append(Escape(line)).Append("\\line ");
					paragraphEnded = false;
					blankLine = false;
					continue;
				}

				if (line.Length is 0)
				{
					BlankLine();
					continue;
				}

				/* a line of HTML - the overview carries its badges and its introduction
				 * that way - is its text, tags removed; what is left of a picture is
				 * nothing, and the line goes */
				if (line.TrimStart().StartsWith("<", StringComparison.Ordinal)) line = StripTags(line);
				/* a page of HTML carries its spaces as entities, and so does Markdown
				 * prose; a line left holding nothing is the space between two pictures
				 * and goes */
				line = Entities(line);
				if (line.Trim().Length is 0) continue;

				blankLine = false;

				if (IsRule(line))
				{
					EndParagraph();
					body.Append("\\pard\\brdrb\\brdrs\\brdrw5\\brsp20 \\par ");
					BodyFace();
					paragraphEnded = true;
					continue;
				}

				if (Heading(line) is { } heading)
				{
					EndParagraph();
					body.Append("\\pard\\sb240\\sa120\\b\\fs").Append(HeadingSize(heading.Level, baseSize)).Append(' ');
					AppendInline(body, heading.Text, fields: fields);
					/* a heading ends its paragraph and is followed by an empty one, so
					 * the text below starts clear of it; the empty one is in the body's
					 * face, or the heading's spacing would be its spacing too */
					body.Append("\\b0 \\par ");
					BodyFace();
					body.Append("\\par ");
					paragraphEnded = true;
					blankLine = true;
					continue;
				}

				if (Quote(line) is { } quote)
				{
					EndParagraph();
					body.Append("\\pard\\li360\\ri180\\i ");
					AppendInline(body, quote, fields: fields);
					body.Append("\\i0 \\par ");
					BodyFace();
					paragraphEnded = true;
					continue;
				}

				if (Bullet(line) is { } bullet)
				{
					EndParagraph();
					body.Append("\\pard\\li360\\fi-180 ");
					body.Append(bullet.Number is { } n ? n + ". " : "\\u8226? ");
					AppendInline(body, bullet.Text, fields: fields);
					body.Append("\\par ");
					BodyFace();
					paragraphEnded = true;
					continue;
				}

				if (line.TrimStart().StartsWith("|", StringComparison.Ordinal))
				{
					/* a table as its rows, in the monospaced face: the columns line up,
					 * which is what a table is for, and nothing has to measure them.
					 * Links and code in a cell are drawn as they are everywhere else. */
					var cells = Cells(line);
					if (cells.Length is 0) continue;   /* the row of dashes under a header */
					EndParagraph();
					body.Append("\\pard\\f1\\fs").Append(codeSize).Append("\\cf3 ");
					AppendInline(body, cells, restoreFont: "\\f1", fields: fields);
					body.Append("\\par ");
					BodyFace();
					paragraphEnded = true;
					continue;
				}

				EndParagraph();
				AppendInline(body, line, fields: fields);
				body.Append("\\par ");
				paragraphEnded = true;
			}

			return "{\\rtf1\\ansi\\ansicpg1252\\uc1\\deff0"
				+ "{\\fonttbl{\\f0\\fswiss Segoe UI;}{\\f1\\fmodern Consolas;}}"
				+ "{\\colortbl;" + Rgb(style.Body) + ";" + Rgb(style.Link) + ";" + Rgb(style.Muted) + ";}"
				+ "\\pard\\f0\\fs" + baseSize + "\\cf1 " + body + "}";
		}

		/// <summary>A heading's size from the body's: 12 pt gives 20, 16, 14 and 13.</summary>
		private static int HeadingSize(int level, int baseSize)
			=> level switch
			{
				1 => baseSize * 5 / 3,
				2 => baseSize * 4 / 3,
				3 => baseSize * 7 / 6,
				_ => baseSize * 13 / 12,
			};

		private static string Rgb(Color c) => $"\\red{c.R}\\green{c.G}\\blue{c.B}";

		/// <summary>The .md files a document links to, as written.</summary>
		public static IReadOnlyList<string> LinksToDocs(string markdown)
		{
			List<string> found = new();
			foreach (var line in Lines(markdown))
			{
				for (var at = line.IndexOf("](", StringComparison.Ordinal); at >= 0;
					at = line.IndexOf("](", at + 2, StringComparison.Ordinal))
				{
					var start = at + 2;
					var end = line.IndexOf(')', start);
					if (end < 0) break;
					var target = line.Substring(start, end - start).Trim();
					if (target.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) found.Add(target);
				}
			}
			return found;
		}

		private static string[] Lines(string markdown)
			=> markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

		private static bool IsRule(string line)
		{
			var t = line.Trim();
			if (t.Length < 3) return false;
			foreach (var c in t)
			{
				if (c is not ('-' or '*' or '_')) return false;
			}
			return true;
		}

		private static (int Level, string Text)? Heading(string line)
		{
			var t = line.TrimStart();
			var hashes = 0;
			while (hashes < t.Length && t[hashes] == '#') hashes++;
			if (hashes is 0 or > 6 || hashes >= t.Length || t[hashes] != ' ') return null;
			return (hashes, t[(hashes + 1)..].Trim());
		}

		private static string? Quote(string line)
		{
			var t = line.TrimStart();
			return t.StartsWith("> ", StringComparison.Ordinal) ? t[2..] : t == ">" ? "" : null;
		}

		private static (string Text, int? Number)? Bullet(string line)
		{
			var t = line.TrimStart();
			if (t.Length > 2 && t[0] is '-' or '*' or '+' && t[1] == ' ') return (t[2..], null);
			var digits = 0;
			while (digits < t.Length && t[digits] >= '0' && t[digits] <= '9') digits++;
			if (digits is > 0 and <= 3 && digits + 1 < t.Length && t[digits] == '.' && t[digits + 1] == ' ')
			{
				return (t[(digits + 2)..], int.Parse(t.Substring(0, digits)));
			}
			return null;
		}

		/// <summary>A table row's cells, joined for a line that lines up in a fixed face.</summary>
		private static string Cells(string line)
		{
			var t = line.Trim();
			if (t.StartsWith("|", StringComparison.Ordinal)) t = t[1..];
			if (t.EndsWith("|", StringComparison.Ordinal)) t = t[..^1];
			var cells = t.Split('|');
			List<string> kept = new();
			foreach (var cell in cells)
			{
				var text = cell.Trim();
				/* the row of dashes that marks a header apart is not a row of anything */
				if (text.Length is not 0 && text.Trim('-', ':', ' ').Length is 0) return "";
				kept.Add(text);
			}
			return string.Join(" | ", kept);
		}

		/// <summary>
		/// The line as RTF runs: inline code, bold, italic and links, with everything
		/// else escaped. Markers left unbalanced are written as the characters they are.
		/// </summary>
		private static void AppendInline(StringBuilder body, string text, bool bold = false, bool italic = false,
			string restoreFont = "\\f0", bool fields = true)
		{
			var at = 0;
			while (at < text.Length)
			{
				var rest = text.Length - at;

				/* a picture is not text: the reader cannot draw it, and the label
				 * would only be a link to a file nobody can open */
				if (rest >= 2 && text[at] == '!' && text[at + 1] == '[')
				{
					var labelEnd = text.IndexOf(']', at + 2);
					if (labelEnd > at && labelEnd + 1 < text.Length && text[labelEnd + 1] is '(' or '[')
					{
						var close = text[labelEnd + 1] == '(' ? ')' : ']';
						var end = text.IndexOf(close, labelEnd + 2);
						if (end > labelEnd)
						{
							at = end + 1;
							continue;
						}
					}
				}

				if (rest >= 2 && text[at] == '`' && text[at + 1] == '`')
				{
					var close = text.IndexOf("``", at + 2, StringComparison.Ordinal);
					if (close > at)
					{
						body.Append("\\f1 ");
						body.Append(Escape(text[(at + 2)..close]));
						body.Append(restoreFont).Append(' ');
						at = close + 2;
						continue;
					}
				}

				if (text[at] == '`')
				{
					var close = text.IndexOf('`', at + 1);
					if (close > at)
					{
						body.Append("\\f1 ");
						body.Append(Escape(text[(at + 1)..close]));
						body.Append(restoreFont).Append(' ');
						at = close + 1;
						continue;
					}
				}

				if (rest >= 4 && text[at] == '*' && text[at + 1] == '*')
				{
					var close = text.IndexOf("**", at + 2, StringComparison.Ordinal);
					if (close > at)
					{
						if (!bold) body.Append("\\b ");
						AppendInline(body, text[(at + 2)..close], true, italic, restoreFont, fields);
						if (!bold) body.Append("\\b0 ");
						at = close + 2;
						continue;
					}
				}

				if (text[at] == '*')
				{
					var close = text.IndexOf('*', at + 1);
					if (close > at + 1)
					{
						if (!italic) body.Append("\\i ");
						AppendInline(body, text[(at + 1)..close], bold, true, restoreFont, fields);
						if (!italic) body.Append("\\i0 ");
						at = close + 1;
						continue;
					}
				}

				if (text[at] == '[')
				{
					var labelEnd = text.IndexOf(']', at + 1);
					if (labelEnd > at && labelEnd + 1 < text.Length && text[labelEnd + 1] == '(')
					{
						var urlEnd = text.IndexOf(')', labelEnd + 2);
						if (urlEnd > labelEnd)
						{
							var label = text[(at + 1)..labelEnd];
							var url = text[(labelEnd + 2)..urlEnd].Trim();
							if (fields)
							{
								body.Append("{\\field{\\*\\fldinst{HYPERLINK \"").Append(EscapeUrl(url))
									.Append("\"}}{\\fldrslt{\\ul\\cf2 ");
								AppendInline(body, label, bold, italic, restoreFont, fields);
								body.Append("}}}");
							}
							else
							{
								/* a reader without fields would draw nothing at all:
								 * the label stands, and the address beside it is what
								 * that reader can turn into a link itself */
								body.Append("{\\ul\\cf2 ");
								AppendInline(body, label, bold, italic, restoreFont, fields);
								if (url.Length is not 0) body.Append(" (").Append(Escape(url)).Append(')');
								body.Append('}');
							}
							at = urlEnd + 1;
							continue;
						}
					}
				}

				var next = at + 1;
				while (next < text.Length && text[next] is not ('`' or '*' or '[')
					&& !(text[next] == '!' && next + 1 < text.Length && text[next + 1] == '[')) next++;
				body.Append(Escape(text[at..next]));
				at = next;
			}
		}

		/// <summary>A line of HTML as the text it holds: tags out, entities read.</summary>
		/// <summary>A line of HTML as the text it holds: the tags taken out.</summary>
		private static string StripTags(string line)
		{
			var sb = new StringBuilder(line.Length);
			var inTag = false;
			foreach (var c in line)
			{
				if (c == '<') { inTag = true; continue; }
				if (c == '>') { inTag = false; continue; }
				if (!inTag) sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>The entities a document may carry, as the characters they mean.</summary>
		private static string Entities(string text)
			=> text
				.Replace("&amp;", "&")
				.Replace("&lt;", "<")
				.Replace("&gt;", ">")
				.Replace("&quot;", "\"")
				.Replace("&#39;", "'")
				.Replace("&nbsp;", " ");

		/// <summary>RTF's own three characters, and anything above ASCII, as RTF writes it.</summary>
		private static string Escape(string text)
		{
			var sb = new StringBuilder(text.Length + 8);
			foreach (var c in text)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '{': sb.Append("\\{"); break;
					case '}': sb.Append("\\}"); break;
					case '\t': sb.Append("\\tab "); break;
					default:
						if (c < 128) sb.Append(c);
						else sb.Append("\\u").Append((int) c).Append('?');
						break;
				}
			}
			return sb.ToString();
		}

		private static string EscapeUrl(string url)
		{
			var sb = new StringBuilder(url.Length);
			foreach (var c in url)
			{
				if (c is '"' or '\\' or '{' or '}') continue;
				if (c < 128) sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
