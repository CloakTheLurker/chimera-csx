using System.Collections.Generic;
using System.IO;
using System.Linq;

using Chimera.Client.Common;
using Chimera.Common.PathExtensions;

namespace Chimera.Tests.Client.Common
{
	/// <summary>
	/// The themes Chimera ships, in the repository's Themes folder, and where the
	/// frontend looks for themes (docs/theming.md).
	///
	/// Every one of them loads - a file the frontend refused would be a theme in
	/// the folder that the window never offers, which nobody would report because
	/// nobody would know it was missing - and every one of them that is not on the
	/// owner's keep list puts readable text on every background, measured as
	/// <see cref="ThemeContrastTests"/> measures Dark, with the WCAG AA floor of
	/// 4.5:1 for text. The twelve kept as their author made them are listed in
	/// <see cref="KeptAsTheyAre"/> (user-decided, 2026-11-23).
	/// </summary>
	[TestClass]
	// CHIMERA_DATA_HOME is process-wide and these tests point it at their own folder; a
	// class running beside them read the other's, which is how a theme went missing once in
	// six runs.
	[DoNotParallelize]
	public class ShippedThemesTests
	{
		/// <summary>
		/// The themes left exactly as their author made them, on the owner's word
		/// (user-decided, 2026-11-23). Dark is compiled in, not a file; the other
		/// eleven are in the folder. A theme here is not measured, so a new one
		/// belongs in this list only if the owner says so.
		/// </summary>
		private static readonly string[] KeptAsTheyAre =
		{
			"Dark", "Argos", "Earl Grey", "Engage", "Espresso", "Evergreen", "Hedgehog",
			"MARPAT", "Nebula", "Pint of Bitter", "Spring Leaves", "Tie-Dye",
		};

		/// <summary>The repository's Themes folder, found by walking up for the file only the repository root has.</summary>
		private static string? ShippedFolder()
		{
			var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
			while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "official-cores.json")))
			{
				dir = dir.Parent;
			}
			return dir is null ? null : Path.Combine(dir.FullName, "Themes");
		}

		[TestMethod]
		public void EveryShippedThemeLoads()
		{
			var folder = ShippedFolder();
			if (folder is null || !Directory.Exists(folder))
			{
				Assert.Inconclusive("the repository is not beside this assembly, so its Themes folder cannot be read");
				return;
			}
			var files = Directory.GetFiles(folder);
			Assert.IsTrue(files.Length >= 60, $"only {files.Length} files in {folder}");

			var (themes, failures) = ThemeLibrary.Read(folder);
			Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Select(static f => f.Message)));
			// two built in, and one theme for every file: none taken for another's name
			Assert.AreEqual(files.Length + 2, themes.Count);

			List<string> wrong = new();
			foreach (var file in files)
			{
				var name = Path.GetFileName(file);
				// a zip carries a name that is not ASCII in a way not every unzipper reads back
				if (name.Any(static c => c > 127)) wrong.Add($"{name}: the file name is not plain ASCII");
				if (!name.EndsWith(ThemeFile.Extension, StringComparison.Ordinal)) wrong.Add($"{name}: a theme file ends in {ThemeFile.Extension}");
			}
			foreach (var theme in themes.Where(static t => !ThemeLibrary.IsBuiltIn(t)))
			{
				if (string.IsNullOrWhiteSpace(theme.Author)) wrong.Add($"{theme.Name}: a theme Chimera ships says who made it");
			}
			Assert.AreEqual(0, wrong.Count, string.Join("\n", wrong));
		}

		/// <summary>
		/// Every shipped theme the owner did not keep as it was puts readable text
		/// on every background. The pairs and the arithmetic are the ones Dark is
		/// held to; what is added is the 4.5:1 that text needs where an icon can
		/// live with 3:1.
		/// </summary>
		[TestMethod]
		public void EveryShippedThemePutsReadableTextOnEveryBackground()
		{
			var folder = ShippedFolder();
			if (folder is null || !Directory.Exists(folder))
			{
				Assert.Inconclusive("the repository is not beside this assembly, so its Themes folder cannot be read");
				return;
			}

			var (themes, failures) = ThemeLibrary.Read(folder);
			Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Select(static f => f.Message)));
			var shipped = themes.Where(static t => !ThemeLibrary.IsBuiltIn(t)).ToList();
			CollectionAssert.IsSubsetOf(
				KeptAsTheyAre.Where(static n => n != "Dark").ToList(),
				shipped.Select(static t => t.Name).ToList(),
				"a theme kept as it is has gone from the folder");

			var pairs = ThemeContrastTests.Pairs().ToList();
			List<string> bad = new();
			var looked = 0;
			foreach (var theme in shipped)
			{
				if (KeptAsTheyAre.Contains(theme.Name, StringComparer.Ordinal)) continue;
				looked++;
				foreach (var (ink, ground) in pairs)
				{
					var ratio = ThemeContrastTests.Ratio(theme[ink], theme[ground]);
					var floor = ThemeContrastTests.FloorFor(ink);
					if (ratio < floor)
					{
						bad.Add($"{theme.Name}: {ink} on {ground} is {ratio:0.0}:1, needs {floor:0.0}:1 "
							+ $"({ThemeFile.FormatColor(theme[ink])} on {ThemeFile.FormatColor(theme[ground])})");
					}
				}
			}

			Assert.AreEqual(shipped.Count - (KeptAsTheyAre.Length - 1), looked,
				"every shipped theme not kept as it is is measured");
			Assert.AreEqual(0, bad.Count, "text that cannot be read off its background:\n" + string.Join("\n", bad));
		}

		/// <summary>
		/// The Themes path in Config &gt; Paths is, by default, the folder the bundle
		/// puts the themes in - beside Chimera.exe, like Cores, Lua and Tools - and
		/// the library's own default is the same folder, so a program that never
		/// read a config (a tool, a test) looks where the frontend does.
		/// </summary>
		[TestMethod]
		public void ThePathsDefaultIsTheFolderBesideTheProgram()
		{
			Config config = new();
			config.ResolveDefaults();
			var expected = Path.GetFullPath(Path.Combine(PathUtils.ExeDirectoryPath, "Themes"));
			Assert.AreEqual(expected, config.PathEntries.ThemesAbsolutePath());
			Assert.AreEqual(expected, Path.GetFullPath(ThemeLibrary.DefaultThemesDirectory));
		}

		private static void WriteTheme(string folder, string file, string name)
		{
			Directory.CreateDirectory(folder);
			var theme = ThemeLibrary.Find("Dark")!;
			File.WriteAllText(Path.Combine(folder, file), ThemeFile.Write(new(name, "", "a test", true, false, file, theme.ToArray())));
		}

		/// <summary>
		/// The folder in Config &gt; Paths is read, and so is the one themes were kept
		/// in before it existed, so that a theme somebody wrote there is not lost.
		/// The same name in both is the configured folder's, and the other file is
		/// reported rather than silently dropped.
		/// </summary>
		[TestMethod]
		public void TheChosenFolderIsReadAndTheFormerOneAfterIt()
		{
			var root = Path.Combine(Path.GetTempPath(), $"chimera-themes-{Guid.NewGuid():N}");
			var chosen = Path.Combine(root, "chosen");
			var dataHome = Path.Combine(root, "data");
			// the data directory is pointed at the test's own through the variable, which
			// wins over everything else and is read on every ask (ProjectCache.DataHome)
			var oldDataHome = Environment.GetEnvironmentVariable("CHIMERA_DATA_HOME");
			try
			{
				WriteTheme(chosen, "Here.chimeraTheme.json", "Here");
				WriteTheme(chosen, "Both.chimeraTheme.json", "Both");
				WriteTheme(Path.Combine(dataHome, "Themes"), "Before.chimeraTheme.json", "Kept From Before");
				WriteTheme(Path.Combine(dataHome, "Themes"), "Both.chimeraTheme.json", "Both");
				Environment.SetEnvironmentVariable("CHIMERA_DATA_HOME", dataHome);

				ThemeLibrary.UseDirectory(chosen);
				ThemeLibrary.Reload();

				Assert.AreEqual(Path.GetFullPath(chosen), Path.GetFullPath(ThemeLibrary.ThemesDirectory));
				var names = ThemeLibrary.All.Select(static t => t.Name).ToList();
				CollectionAssert.Contains(names, "Here");
				CollectionAssert.Contains(names, "Kept From Before");
				Assert.AreEqual(1, names.Count(static n => n == "Both"));
				var failure = ThemeLibrary.Failures.Single();
				StringAssert.StartsWith(Path.GetFullPath(failure.Path), Path.GetFullPath(dataHome));
				StringAssert.Contains(failure.Message, "already a theme called \"Both\"");

				// the same folder again is not a reason to read anything; another one is
				Assert.IsFalse(ThemeLibrary.UseDirectory(chosen));
				Assert.IsTrue(ThemeLibrary.UseDirectory(Path.Combine(root, "elsewhere")));
				CollectionAssert.DoesNotContain(ThemeLibrary.All.Select(static t => t.Name).ToList(), "Here");
			}
			finally
			{
				Environment.SetEnvironmentVariable("CHIMERA_DATA_HOME", oldDataHome);
				ThemeLibrary.UseDirectory("");
				ThemeLibrary.Reload();
				ThemeLibrary.Select(ThemeLibrary.FallbackThemeName);
				try { Directory.Delete(root, recursive: true); } catch (IOException) { }
			}
		}
	}
}
