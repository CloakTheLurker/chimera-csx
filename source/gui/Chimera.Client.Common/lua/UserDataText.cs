#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chimera.Client.Common
{
	/// <summary>
	/// The scripts' userdata as one text, kept with a branch (issue 534). One
	/// line a value: its kind, its escaped name, its value. Kinds: b a boolean,
	/// i a whole number, u an unsigned whole number, f a number with a fraction
	/// (its 64 bits in hex), s text, each value keeping its kind
	/// when it comes back.
	///
	/// Everything but a letter, a digit, a dot, an underscore or a hyphen is
	/// written %XX, in a name or in text, so a key or a value may hold anything.
	/// The format is Chimera CSX's own: upstream's text uses %a for fractions,
	/// which .NET does not write; a project's text is read back by the engine's
	/// counterpart, not parsed by it.
	/// </summary>
	public static class UserDataText
	{
		public static string Serialize(IEnumerable<KeyValuePair<string, object?>> bag)
		{
			var sb = new StringBuilder();
			foreach (var pair in bag)
			{
				var key = Escape(pair.Key);
				string? line = pair.Value switch
				{
					null => null, // a null is not a value a script can give back
					bool b => $"b {key} {(b ? "true" : "false")}",
					_ => pair.Value switch
					{
						string s => $"s {key} {Escape(s)}",
						sbyte or short or int or long or byte or ushort or uint =>
							$"i {key} {Convert.ToInt64(pair.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}",
						ulong u => $"u {key} {u.ToString(CultureInfo.InvariantCulture)}",
						float f => $"f {key} {FractionBits(f)}",
						double d => $"f {key} {FractionBits(d)}",
						char c => $"s {key} {Escape(c.ToString())}",
						_ => $"s {key} {Escape(Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? "")}",
					},
				};
				if (line is null) continue;
				if (sb.Length > 0) sb.Append('\n');
				sb.Append(line);
			}
			return sb.ToString();
		}

		public static List<KeyValuePair<string, object>> Parse(string? text)
		{
			List<KeyValuePair<string, object>> bag = new();
			if (string.IsNullOrEmpty(text)) return bag;
			foreach (var line in text.Split('\n'))
			{
				var parts = line.Split(' ', 3);
				if (parts.Length < 2) continue;
				var key = Unescape(parts[1]);
				var value = parts.Length > 2 ? parts[2] : "";
				switch (parts[0])
				{
					case "b":
						bag.Add(new(key, value == "true"));
						break;
					case "s":
						bag.Add(new(key, Unescape(value)));
						break;
					case "i":
						if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
							bag.Add(new(key, whole));
						break;
					case "u":
						if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsigned))
							bag.Add(new(key, unsigned));
						break;
					case "f":
						if (ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits))
							bag.Add(new(key, BitConverter.Int64BitsToDouble(unchecked((long)bits))));
						break;
				}
			}
			return bag;
		}

		/// <summary>A fraction's 64 bits, hex: -0.0, NaN and the infinities keep exactly what they were.</summary>
		private static string FractionBits(double value)
			=> BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

		private static string Escape(string text)
		{
			var sb = new StringBuilder(text.Length);
			foreach (var b in Encoding.UTF8.GetBytes(text))
			{
				var c = (char)b;
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c is '.' or '_' or '-')
					sb.Append(c);
				else
					sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
			}
			return sb.ToString();
		}

		private static string Unescape(string text)
		{
			var bytes = new List<byte>(text.Length);
			for (var i = 0; i < text.Length; i++)
			{
				if (text[i] == '%' && i + 2 < text.Length
					&& byte.TryParse(text.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
				{
					bytes.Add(b);
					i += 2;
				}
				else
				{
					bytes.Add((byte)text[i]);
				}
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}
	}

	/// <summary>
	/// The one place the Lua engine keeps a branch's scripts' userdata text
	/// (issue 534): TAStudio asks for it when a branch is made, and gives it
	/// back before event.onloadstate when one is loaded. No Lua engine, no
	/// provider, and branches keep none.
	/// </summary>
	public static class ScriptUserData
	{
		private static object? _owner;
		private static Func<string?>? _capture;
		private static Action<string?>? _restore;

		public static void Set(object owner, Func<string?> capture, Action<string?> restore)
		{
			_owner = owner;
			_capture = capture;
			_restore = restore;
		}

		public static void Clear(object owner)
		{
			if (!ReferenceEquals(_owner, owner)) return;
			_owner = null;
			_capture = null;
			_restore = null;
		}

		/// <summary>The current bag as text, or null when no Lua engine is open.</summary>
		public static string? CaptureText() => _capture?.Invoke();

		/// <summary>Puts a branch's bag back; null leaves the bag as it is.</summary>
		public static void RestoreText(string? text)
		{
			if (text is not null) _restore?.Invoke(text);
		}
	}
}

