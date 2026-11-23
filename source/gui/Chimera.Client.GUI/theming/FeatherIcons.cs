#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Chimera.Common;

namespace Chimera.Client.GUI
{
	/// <summary>
	/// The Feather icon set (https://feathericons.com, MIT), drawn from the SVG
	/// files under <c>images/feather</c>, and the few drawings the project adds
	/// where Feather has no glyph (the gamepad), under <c>images/glyphs</c>.
	///
	/// Feather's icons are 24-unit line drawings, so they are drawn here rather
	/// than shipped as pictures: one renderer gives every size the window asks
	/// for - 16 in a menu, 32 for a form's title bar - and one ink colour for the
	/// whole set, which is what makes them look like a set. The SVG files are the
	/// originals the project publishes, so a glyph can be replaced by replacing
	/// its file.
	///
	/// Rendering is done at four times the requested size and scaled down, the
	/// same way the hand-drawn menu icons were: a 16-pixel line drawing made at
	/// 16 pixels looks like an accident.
	/// </summary>
	public static class FeatherIcons
	{
		private const string ResourcePrefix = "Chimera.Client.GUI.images.feather.";
		private const string GlyphPrefix = "Chimera.Client.GUI.images.glyphs.";
		private const float DesignSize = 24f;
		private const int Supersample = 4;

		/// <summary>
		/// The one colour every icon is drawn in: a mid tone that can be seen on
		/// the white menu of the desktop theme and on the near-black menu of a dark
		/// one (MenuIconTests measures it). A few status icons pass another colour.
		/// </summary>
		public static readonly Color Ink = Color.FromArgb(0x6B, 0x72, 0x80);

		private static readonly object Gate = new();
		private static readonly Dictionary<string, XDocument> Documents = new(StringComparer.Ordinal);
		private static readonly Dictionary<(string Name, int Size, int Argb), Bitmap> Bitmaps = new();

		/// <summary>One icon as a bitmap of <paramref name="size"/> pixels a side.</summary>
		public static Bitmap Image(string name, int size = 16, Color? ink = null)
		{
			var colour = ink ?? Ink;
			var key = (name, size, colour.ToArgb());
			lock (Gate)
			{
				if (Bitmaps.TryGetValue(key, out var cached)) return cached;
			}

			var bitmap = Render(Document(name), size, colour);
			lock (Gate)
			{
				Bitmaps[key] = bitmap;
			}
			return bitmap;
		}

		/// <summary>
		/// One icon as a form icon: 16 and 32 pixel images in one ICO, so the title
		/// bar and the Alt-Tab list each get the size they want.
		/// </summary>
		public static Icon Icon(string name, Color? ink = null)
		{
			// the bitmaps are cached and shared, so they are not disposed here
			var small = Image(name, 16, ink);
			var large = Image(name, 32, ink);
			return new Icon(new MemoryStream(Ico(small, large)));
		}

		private static XDocument Document(string name)
		{
			lock (Gate)
			{
				if (Documents.TryGetValue(name, out var cached)) return cached;
			}
			Stream stream;
			try
			{
				stream = typeof(FeatherIcons).Assembly.EmbeddedResourceStream(ResourcePrefix + name + ".svg");
			}
			catch (ArgumentException)
			{
				stream = typeof(FeatherIcons).Assembly.EmbeddedResourceStream(GlyphPrefix + name + ".svg");
			}
			using (stream)
			{
				var document = XDocument.Load(stream);
				lock (Gate)
				{
					Documents[name] = document;
				}
				return document;
			}
		}

		private static Bitmap Render(XDocument document, int size, Color ink)
		{
			var root = document.Root!;
			var (designWidth, designHeight) = DesignSizeOf(root);
			var filled = root.Attribute("stroke") is null || root.Attribute("stroke")!.Value == "none";
			var big = size * Supersample;
			using var large = new Bitmap(big, big);
			using (var g = Graphics.FromImage(large))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.Clear(Color.Transparent);
				var scale = Math.Min(big / designWidth, big / designHeight);
				using var pen = new Pen(ink, Math.Max(1f, 2.4f * scale))
				{
					StartCap = LineCap.Round,
					EndCap = LineCap.Round,
					LineJoin = LineJoin.Round,
				};
				using var brush = new SolidBrush(ink);
				foreach (var element in root.Elements())
				{
					using var shape = Shape(element, scale);
					if (shape is null) continue;
					if (filled) g.FillPath(brush, shape);
					else g.DrawPath(pen, shape);
				}
			}

			var icon = new Bitmap(size, size);
			using (var g = Graphics.FromImage(icon))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.Clear(Color.Transparent);
				g.DrawImage(large, new Rectangle(0, 0, size, size), 0, 0, big, big, GraphicsUnit.Pixel);
			}

			// A Feather line is two units in a 24-unit drawing, which is thinner than
			// a pixel at 16 pixels a side once it is scaled down: the stroke's own
			// pixels come out half transparent and a menu icon looks like a smudge.
			// Opaque it up rather than widen it, so the drawing keeps Feather's shape.
			for (var y = 0; y < size; y++)
			{
				for (var x = 0; x < size; x++)
				{
					var colour = icon.GetPixel(x, y);
					if (colour.A is > 0 and < 255)
						icon.SetPixel(x, y, Color.FromArgb(Math.Min(255, (int) (colour.A * 1.35)), colour));
				}
			}
			return icon;
		}

		private static (float Width, float Height) DesignSizeOf(XElement root)
		{
			var viewBox = root.Attribute("viewBox")?.Value;
			if (!string.IsNullOrWhiteSpace(viewBox))
			{
				var parts = viewBox.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 4
					&& float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)
					&& float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
					&& w > 0 && h > 0)
				{
					return (w, h);
				}
			}
			var width = Length(root.Attribute("width")?.Value) ?? DesignSize;
			var height = Length(root.Attribute("height")?.Value) ?? width;
			return (width, height);
		}

		private static float? Length(string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return null;
			var cleaned = text!.Trim().TrimEnd('p', 'x', 'X', 'P');
			return float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
				? value
				: (float?) null;
		}

		private static GraphicsPath? Shape(XElement element, float scale)
		{
			var path = new GraphicsPath();
			switch (element.Name.LocalName)
			{
				case "line":
					path.AddLine(Point(element, "x1", scale, "y1"), Point(element, "x2", scale, "y2"));
					return path;

				case "circle":
				{
					float cx = Number(element, "cx") * scale;
					float cy = Number(element, "cy") * scale;
					float r = Number(element, "r") * scale;
					path.AddEllipse(cx - r, cy - r, 2 * r, 2 * r);
					return path;
				}

				case "ellipse":
				{
					float cx = Number(element, "cx") * scale;
					float cy = Number(element, "cy") * scale;
					float rx = Number(element, "rx") * scale;
					float ry = Number(element, "ry") * scale;
					path.AddEllipse(cx - rx, cy - ry, 2 * rx, 2 * ry);
					return path;
				}

				case "rect":
					return RoundedRect(element, scale);

				case "polyline":
				case "polygon":
				{
					var points = Points(element.Attribute("points")?.Value ?? string.Empty, scale);
					if (points.Count < 2) return null;
					path.AddLines(points.ToArray());
					if (element.Name.LocalName == "polygon") path.CloseFigure();
					return path;
				}

				case "path":
					return PathData(element.Attribute("d")?.Value ?? string.Empty, scale);

				default:
					path.Dispose();
					return null;
			}
		}

		private static GraphicsPath RoundedRect(XElement element, float scale)
		{
			var path = new GraphicsPath();
			float x = Number(element, "x") * scale;
			float y = Number(element, "y") * scale;
			float w = Number(element, "width") * scale;
			float h = Number(element, "height") * scale;
			float rx = Number(element, "rx", 0f) * scale;
			float ry = Number(element, "ry", 0f) * scale;
			if (rx <= 0f && ry <= 0f)
			{
				path.AddRectangle(new RectangleF(x, y, w, h));
				return path;
			}
			rx = Math.Min(rx == 0f ? ry : rx, w / 2f);
			ry = Math.Min(ry == 0f ? rx : ry, h / 2f);
			float d = 180f;
			path.AddArc(x, y, 2 * rx, 2 * ry, d, 90);
			path.AddArc(x + w - (2 * rx), y, 2 * rx, 2 * ry, 270, 90);
			path.AddArc(x + w - (2 * rx), y + h - (2 * ry), 2 * rx, 2 * ry, 0, 90);
			path.AddArc(x, y + h - (2 * ry), 2 * rx, 2 * ry, 90, 90);
			path.CloseFigure();
			return path;
		}

		private static PointF Point(XElement element, string xName, float scale, string yName)
			=> new(Number(element, xName) * scale, Number(element, yName) * scale);

		private static float Number(XElement element, string name, float fallback = 0f)
		{
			var attribute = element.Attribute(name);
			return attribute is null || !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
				? fallback
				: value;
		}

		private static List<PointF> Points(string text, float scale)
		{
			var numbers = Regex.Matches(text, @"[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?");
			var points = new List<PointF>();
			for (var i = 0; i + 1 < numbers.Count; i += 2)
			{
				points.Add(new PointF(
					float.Parse(numbers[i].Value, CultureInfo.InvariantCulture) * scale,
					float.Parse(numbers[i + 1].Value, CultureInfo.InvariantCulture) * scale));
			}
			return points;
		}

		private static readonly Regex PathTokens = new(
			@"[MmLlHhVvCcSsQqTtAaZz]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?",
			RegexOptions.Compiled);

		/// <summary>
		/// An SVG path as a GraphicsPath. Curves and arcs are flattened into short
		/// lines, which is enough at four times the size: the drawing is scaled down
		/// afterwards, so the corners of a flattening never reach the screen.
		/// </summary>
		private static GraphicsPath PathData(string data, float scale)
		{
			var path = new GraphicsPath();
			var tokens = PathTokens.Matches(data);
			var index = 0;
			char command = 'M';
			char previous = 'M';
			var current = PointF.Empty;
			var start = PointF.Empty;
			var control = PointF.Empty;
			var quadratic = PointF.Empty;
			var points = new List<PointF>();

			float Next() => float.Parse(tokens[index++].Value, CultureInfo.InvariantCulture) * scale;
			float Raw() => float.Parse(tokens[index++].Value, CultureInfo.InvariantCulture);
			void Flush(bool closed)
			{
				if (points.Count >= 2)
				{
					path.AddLines(points.ToArray());
					if (closed) path.CloseFigure();
				}
				points.Clear();
			}

			while (index < tokens.Count)
			{
				if (char.IsLetter(tokens[index].Value[0]))
				{
					command = tokens[index].Value[0];
					index++;
				}
				else if (previous == 'M')
				{
					command = 'L';
				}
				else if (previous == 'm')
				{
					command = 'l';
				}

				switch (command)
				{
					case 'M':
					case 'm':
					{
						Flush(false);
						var relative = command == 'm';
						var x = Next();
						var y = Next();
						current = relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
						start = current;
						points.Add(current);
						break;
					}

					case 'L':
					case 'l':
					{
						var relative = command == 'l';
						var x = Next();
						var y = Next();
						current = relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
						points.Add(current);
						break;
					}

					case 'H':
					case 'h':
					{
						var x = Next();
						current = new PointF(command == 'h' ? current.X + x : x, current.Y);
						points.Add(current);
						break;
					}

					case 'V':
					case 'v':
					{
						var y = Next();
						current = new PointF(current.X, command == 'v' ? current.Y + y : y);
						points.Add(current);
						break;
					}

					case 'C':
					case 'c':
					{
						var relative = command == 'c';
						var c1 = Fetch(Next, Next, current, relative);
						var c2 = Fetch(Next, Next, current, relative);
						var end = Fetch(Next, Next, current, relative);
						Curve(points, current, c1, c2, end);
						control = c2;
						current = end;
						break;
					}

					case 'S':
					case 's':
					{
						var relative = command == 's';
						var c1 = previous is 'C' or 'c' or 'S' or 's'
							? new PointF((2 * current.X) - control.X, (2 * current.Y) - control.Y)
							: current;
						var c2 = Fetch(Next, Next, current, relative);
						var end = Fetch(Next, Next, current, relative);
						Curve(points, current, c1, c2, end);
						control = c2;
						current = end;
						break;
					}

					case 'Q':
					case 'q':
					{
						var relative = command == 'q';
						var c = Fetch(Next, Next, current, relative);
						var end = Fetch(Next, Next, current, relative);
						Quadratic(points, current, c, end);
						quadratic = c;
						current = end;
						break;
					}

					case 'T':
					case 't':
					{
						var relative = command == 't';
						var c = previous is 'Q' or 'q' or 'T' or 't'
							? new PointF((2 * current.X) - quadratic.X, (2 * current.Y) - quadratic.Y)
							: current;
						var end = Fetch(Next, Next, current, relative);
						Quadratic(points, current, c, end);
						quadratic = c;
						current = end;
						break;
					}

					case 'A':
					case 'a':
					{
						var relative = command == 'a';
						var rx = Next();
						var ry = Next();
						var rotation = Raw();
						var large = Raw() != 0f;
						var sweep = Raw() != 0f;
						var end = Fetch(Next, Next, current, relative);
						Arc(points, current, end, rx, ry, rotation, large, sweep);
						current = end;
						break;
					}

					case 'Z':
					case 'z':
						Flush(true);
						current = start;
						points.Add(current);
						break;
				}

				previous = command;
			}

			Flush(false);
			return path;
		}

		private static PointF Fetch(Func<float> next, Func<float> next2, PointF current, bool relative)
		{
			var x = next();
			var y = next2();
			return relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
		}

		private static void Curve(List<PointF> points, PointF p0, PointF p1, PointF p2, PointF p3)
		{
			const int Steps = 24;
			for (var i = 1; i <= Steps; i++)
			{
				var t = i / (float) Steps;
				var u = 1 - t;
				points.Add(new PointF(
					(u * u * u * p0.X) + (3 * u * u * t * p1.X) + (3 * u * t * t * p2.X) + (t * t * t * p3.X),
					(u * u * u * p0.Y) + (3 * u * u * t * p1.Y) + (3 * u * t * t * p2.Y) + (t * t * t * p3.Y)));
			}
		}

		private static void Quadratic(List<PointF> points, PointF p0, PointF p1, PointF p2)
		{
			const int Steps = 20;
			for (var i = 1; i <= Steps; i++)
			{
				var t = i / (float) Steps;
				var u = 1 - t;
				points.Add(new PointF(
					(u * u * p0.X) + (2 * u * t * p1.X) + (t * t * p2.X),
					(u * u * p0.Y) + (2 * u * t * p1.Y) + (t * t * p2.Y)));
			}
		}

		private static void Arc(List<PointF> points, PointF from, PointF to, float rx, float ry,
			float rotation, bool large, bool sweep)
		{
			if (rx == 0f || ry == 0f || (from.X == to.X && from.Y == to.Y))
			{
				points.Add(to);
				return;
			}
			rx = Math.Abs(rx);
			ry = Math.Abs(ry);
			var phi = rotation * Math.PI / 180.0;
			var cosPhi = Math.Cos(phi);
			var sinPhi = Math.Sin(phi);

			var dx = (from.X - to.X) / 2.0;
			var dy = (from.Y - to.Y) / 2.0;
			var x1 = (cosPhi * dx) + (sinPhi * dy);
			var y1 = (-sinPhi * dx) + (cosPhi * dy);
			var lambda = ((x1 * x1) / (rx * rx)) + ((y1 * y1) / (ry * ry));
			if (lambda > 1)
			{
				var grow = Math.Sqrt(lambda);
				rx = (float) (rx * grow);
				ry = (float) (ry * grow);
			}

			var numerator = (rx * rx * ry * ry) - (rx * rx * y1 * y1) - (ry * ry * x1 * x1);
			var denominator = (rx * rx * y1 * y1) + (ry * ry * x1 * x1);
			var factor = denominator == 0 ? 0 : Math.Sqrt(Math.Max(0, numerator / denominator));
			if (large == sweep) factor = -factor;
			var cx1 = factor * rx * y1 / ry;
			var cy1 = -factor * ry * x1 / rx;
			var cx = (cosPhi * cx1) - (sinPhi * cy1) + ((from.X + to.X) / 2.0);
			var cy = (sinPhi * cx1) + (cosPhi * cy1) + ((from.Y + to.Y) / 2.0);

			var ux = (x1 - cx1) / rx;
			var uy = (y1 - cy1) / ry;
			var vx = (-x1 - cx1) / rx;
			var vy = (-y1 - cy1) / ry;
			var startAngle = Math.Atan2(uy, ux);
			var delta = Math.Atan2((ux * vy) - (uy * vx), (ux * vx) + (uy * vy));
			if (!sweep && delta > 0) delta -= 2 * Math.PI;
			else if (sweep && delta < 0) delta += 2 * Math.PI;

			var steps = Math.Max(8, (int) Math.Ceiling(Math.Abs(delta) / (Math.PI / 18)));
			for (var i = 1; i <= steps; i++)
			{
				var angle = startAngle + (delta * i / steps);
				var x = cx + (rx * Math.Cos(angle) * cosPhi) - (ry * Math.Sin(angle) * sinPhi);
				var y = cy + (rx * Math.Cos(angle) * sinPhi) + (ry * Math.Sin(angle) * cosPhi);
				points.Add(new PointF((float) x, (float) y));
			}
		}

		/// <summary>
		/// An ICO holding the 16 and 32 pixel images, written as 32-bit DIBs: the
		/// form every Windows and every Mono understands, unlike a PNG inside an
		/// ICO, which only newer Windows can read.
		/// </summary>
		private static byte[] Ico(Bitmap small, Bitmap large)
		{
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);
			writer.Write((ushort) 0);
			writer.Write((ushort) 1);
			writer.Write((ushort) 2);
			var images = new[] { small, large };
			var offset = 6 + (16 * images.Length);
			var payloads = new List<byte[]>();
			foreach (var image in images)
			{
				var payload = Dib(image);
				payloads.Add(payload);
				writer.Write((byte) image.Width);
				writer.Write((byte) image.Height);
				writer.Write((byte) 0);
				writer.Write((byte) 0);
				writer.Write((ushort) 1);
				writer.Write((ushort) 32);
				writer.Write(payload.Length);
				writer.Write(offset);
				offset += payload.Length;
			}
			foreach (var payload in payloads) writer.Write(payload);
			writer.Flush();
			return stream.ToArray();
		}

		private static byte[] Dib(Bitmap image)
		{
			var width = image.Width;
			var height = image.Height;
			var stride = width * 4;
			var maskStride = ((width + 31) / 32) * 4;
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);
			writer.Write(40);
			writer.Write(width);
			writer.Write(height * 2);
			writer.Write((ushort) 1);
			writer.Write((ushort) 32);
			writer.Write(0);
			writer.Write(stride * height);
			writer.Write(0);
			writer.Write(0);
			writer.Write(0);
			writer.Write(0);
			for (var y = height - 1; y >= 0; y--)
			{
				for (var x = 0; x < width; x++)
				{
					var c = image.GetPixel(x, y);
					writer.Write(c.B);
					writer.Write(c.G);
					writer.Write(c.R);
					writer.Write(c.A);
				}
			}
			for (var y = 0; y < height; y++) writer.Write(new byte[maskStride]);
			writer.Flush();
			return stream.ToArray();
		}
	}
}
