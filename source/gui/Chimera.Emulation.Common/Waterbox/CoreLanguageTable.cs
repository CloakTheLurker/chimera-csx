#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json.Linq;

namespace Chimera.Emulation.Common.Waterbox
{
	/// <summary>
	/// A core package's own words, <c>languages/en.json</c>. Packages built after
	/// nightly-2026-10-27 name settings, file slots and firmware by key and carry
	/// the words here; a package built before that holds the words themselves and
	/// has no table, so nothing is looked up for it.
	///
	/// The English table is the one this window reads: the interface is English,
	/// and a key with no entry keeps the key, which is what an older frontend
	/// showed for a newer package.
	/// </summary>
	public static class CoreLanguageTable
	{
		/// <summary>The table's path inside a package and in its extracted folder.</summary>
		public const string EnglishPath = "languages/en.json";

		/// <summary>The "texts" object of a table, or null when there is none to read.</summary>
		public static IReadOnlyDictionary<string, string>? Parse(string? json)
		{
			if (string.IsNullOrWhiteSpace(json)) return null;
			try
			{
				if (JObject.Parse(json)["texts"] is not JObject texts) return null;
				Dictionary<string, string> table = new(StringComparer.Ordinal);
				foreach (var property in texts.Properties())
				{
					if (property.Value.Type == JTokenType.String && property.Value.Value<string>() is { } word)
					{
						table[property.Name] = word;
					}
				}
				return table.Count is 0 ? null : table;
			}
			catch (Newtonsoft.Json.JsonException)
			{
				return null;
			}
		}

		/// <summary>The table in an extracted package folder, or null when it has none.</summary>
		public static IReadOnlyDictionary<string, string>? FromDirectory(string packageDir)
		{
			var path = Path.Combine(packageDir, "languages", "en.json");
			return File.Exists(path) ? Parse(File.ReadAllText(path)) : null;
		}

		/// <summary>The word for a key, or the text itself when it is not one.</summary>
		public static string? Resolve(IReadOnlyDictionary<string, string>? texts, string? text)
			=> text is not null && texts is not null && texts.TryGetValue(text, out var word) ? word : text;
	}
}
