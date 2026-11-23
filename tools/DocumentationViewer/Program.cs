using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Chimera.Client.GUI;

namespace Chimera.DocumentationViewer
{
	/// <summary>
	/// The documentation reader on its own, for working on it: the same
	/// DocumentationForm the Help menu opens, with Markdown files from disk in
	/// place of the resources embedded in the window.
	///
	///   DocumentationViewer.exe                 the repository's README and docs/*.md
	///   DocumentationViewer.exe a.md b.md       those files
	///
	/// Links between the files it loaded open them, as they do in the window; any
	/// other link goes to the browser. This is a development tool and is not part
	/// of a bundle.
	/// </summary>
	internal static class Program
	{
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var files = args.Length != 0 ? args.Where(File.Exists).ToList() : RepositoryDocs();
			using var form = new DocumentationForm(loadShipped: false);
			foreach (var file in files)
			{
				try
				{
					form.LoadMarkdown(Path.GetFileName(file), File.ReadAllText(file));
				}
				catch (IOException e)
				{
					form.LoadMarkdown(Path.GetFileName(file), $"# {Path.GetFileName(file)}\n\n{e.Message}\n");
				}
			}

			if (files.Count is not 0) form.ShowTopic(0);
			Application.Run(form);
		}

		/// <summary>
		/// The README and every docs/*.md above the executable, or the current
		/// folder when it is run somewhere else: a source checkout, which is where
		/// this is used.
		/// </summary>
		private static List<string> RepositoryDocs()
		{
			foreach (var start in new[] { AppDomain.CurrentDomain.BaseDirectory, Environment.CurrentDirectory })
			{
				for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
				{
					var docs = Path.Combine(dir.FullName, "docs");
					if (!File.Exists(Path.Combine(docs, "README.md"))) continue;
					List<string> files = new();
					var readme = Path.Combine(dir.FullName, "README.md");
					if (File.Exists(readme)) files.Add(readme);
					files.AddRange(Directory.GetFiles(docs, "*.md", SearchOption.TopDirectoryOnly)
						.OrderBy(static f => f, StringComparer.OrdinalIgnoreCase));
					return files;
				}
			}
			return new List<string>();
		}
	}
}
