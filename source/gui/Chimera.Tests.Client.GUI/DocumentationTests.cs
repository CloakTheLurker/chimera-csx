#nullable enable

using System;
using System.IO;
using System.Linq;

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
