#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Chimera.Client.Common
{
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
		public static string ToRtf(string markdown)
		{
			var body = new StringBuilder();
			var inFence = false;

			foreach (var raw in Lines(markdown))
			{
				var line = raw.TrimEnd();

				if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
				{
					inFence = !inFence;
					body.Append(inFence ? "\\pard\\f1\\fs18\\cf3 " : "\\pard\\f0\\fs20\\cf1 ");
					continue;
				}

				if (inFence)
				{
					body.Append(Escape(line)).Append("\\line ");
					continue;
				}

				if (line.Length is 0)
				{
					Paragraph(body);
					continue;
				}

				/* a line of HTML - the overview carries its badges and its introduction
				 * that way - is its text, tags removed; what is left of a picture is
				 * nothing, and the line goes */
				if (line.TrimStart().StartsWith("<", StringComparison.Ordinal))
				{
					var stripped = StripHtml(line);
					if (stripped.Trim().Length is 0) continue;
					line = stripped;
				}

				if (IsRule(line))
				{
					Paragraph(body);
					body.Append("\\pard\\brdrb\\brdrs\\brdrw5\\brsp20 \\par");
					continue;
				}

				if (Heading(line) is { } heading)
				{
					Paragraph(body);
					var size = heading.Level switch { 1 => 32, 2 => 26, 3 => 22, _ => 20 };
					body.Append("\\pard\\sb180\\sa60\\b\\fs").Append(size).Append(' ');
					AppendInline(body, heading.Text);
					body.Append("\\b0\\fs20 \\par");
					continue;
				}

				if (Quote(line) is { } quote)
				{
					Paragraph(body);
					body.Append("\\pard\\li360\\ri180\\i ");
					AppendInline(body, quote);
					body.Append("\\i0 \\par");
					continue;
				}

				if (Bullet(line) is { } bullet)
				{
					Paragraph(body);
					body.Append("\\pard\\li360\\fi-180 ");
					body.Append(bullet.Number is { } n ? n + ". " : "\\u8226? ");
					AppendInline(body, bullet.Text);
					body.Append("\\par");
					continue;
				}

				if (line.TrimStart().StartsWith("|", StringComparison.Ordinal))
				{
					/* a table as its rows, in the monospaced face: the columns line up,
					 * which is what a table is for, and nothing has to measure them */
					var cells = Cells(line);
					if (cells.Length is 0) continue;   /* the row of dashes under a header */
					Paragraph(body);
					body.Append("\\pard\\f1\\fs18\\cf3 ");
					body.Append(Escape(cells)).Append("\\par");
					body.Append("\\pard\\f0\\fs20\\cf1 ");
					continue;
				}

				AppendInline(body, line);
				body.Append("\\par");
			}

			return "{\\rtf1\\ansi\\ansicpg1252\\uc1\\deff0"
				+ "{\\fonttbl{\\f0\\fswiss Segoe UI;}{\\f1\\fmodern Consolas;}}"
				+ "{\\colortbl;\\red32\\green32\\blue32;\\red70\\green110\\blue175;\\red110\\green110\\blue110;}"
				+ "\\pard\\f0\\fs20\\cf1 " + body + "}";
		}

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

		/// <summary>Ends the paragraph in hand, once: two in a row say nothing more than one.</summary>
		private static void Paragraph(StringBuilder body)
		{
			if (body.Length is 0) return;
			if (body.Length >= 4 && body.ToString(body.Length - 4, 4) == "\\par") return;
			body.Append("\\par");
		}

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
		private static void AppendInline(StringBuilder body, string text, bool bold = false, bool italic = false)
		{
			var at = 0;
			while (at < text.Length)
			{
				var rest = text.Length - at;

				if (rest >= 2 && text[at] == '`' && text[at + 1] == '`')
				{
					var close = text.IndexOf("``", at + 2, StringComparison.Ordinal);
					if (close > at)
					{
						body.Append("\\f1 ");
						body.Append(Escape(text[(at + 2)..close]));
						body.Append("\\f0 ");
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
						body.Append("\\f0 ");
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
						AppendInline(body, text[(at + 2)..close], true, italic);
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
						AppendInline(body, text[(at + 1)..close], bold, true);
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
							body.Append("{\\field{\\*\\fldinst{HYPERLINK \"").Append(EscapeUrl(url))
								.Append("\"}}{\\fldrslt{\\ul\\cf2 ");
							AppendInline(body, label, bold, italic);
							body.Append("}}}");
							at = urlEnd + 1;
							continue;
						}
					}
				}

				var next = at + 1;
				while (next < text.Length && text[next] is not ('`' or '*' or '[')) next++;
				body.Append(Escape(text[at..next]));
				at = next;
			}
		}

		/// <summary>A line of HTML as the text it holds: tags out, entities read.</summary>
		private static string StripHtml(string line)
		{
			var sb = new StringBuilder(line.Length);
			var inTag = false;
			foreach (var c in line)
			{
				if (c == '<') { inTag = true; continue; }
				if (c == '>') { inTag = false; continue; }
				if (!inTag) sb.Append(c);
			}
			return sb.ToString()
				.Replace("&amp;", "&")
				.Replace("&lt;", "<")
				.Replace("&gt;", ">")
				.Replace("&quot;", "\"")
				.Replace("&#39;", "'")
				.Replace("&nbsp;", " ");
		}

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
