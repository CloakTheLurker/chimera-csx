#nullable enable

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;

using Chimera.Client.GUI.Properties;

namespace Chimera.Client.GUI
{
	/// <summary>
	/// Every image that goes beside a menu item, in one place.
	///
	/// It is one place for two reasons. A menu where half the items have an icon
	/// looks broken, so the gaps have to be visible as gaps - and they are, by
	/// reading down this file. And an icon has to be legible on a WHITE menu and
	/// on a very dark one, which is not something anybody will check by eye every
	/// time; MenuIconTests walks these by reflection and measures them against
	/// every theme's menu colour, so an icon that cannot be seen is a failing
	/// test rather than a bug report.
	///
	/// They are Feather's line drawings (theming/FeatherIcons.cs), rendered at the
	/// size a menu wants and in one mid tone: that tone is what keeps a glyph
	/// visible on a white menu and on a near-black one, where a dark outline
	/// vanishes on one of the two. The exceptions are Chimera's own logo and the
	/// coloured status pictures, which are not commands.
	///
	/// Not everything gets one, on purpose:
	/// <list type="bullet">
	/// <item>A menu item that can be TICKED shows its tick in the image margin,
	/// so an image there fights the tick. Every Display-this toggle, every
	/// throttle mode, every window scale is deliberately bare.</item>
	/// <item>A label that is not clickable - "Loaded core: ...", the movie status
	/// line - is not a command and does not get a command's icon.</item>
	/// <item>Exit, which sits alone after a separator, and which no icon says
	/// anything useful about.</item>
	/// </list>
	/// </summary>
	public static class MenuIcons
	{
		// ---- the ones the frontend already had -----------------------------

		public static Image CloseProject => Resources.Close;

		public static Image EncodeVideo => Resources.Avi;

		public static Image Screenshot => Resources.Camera;

		public static Image Pause => Resources.Pause;

		public static Image RebootCore => Resources.Reboot;

		public static Image Fullscreen => Resources.Fullscreen;

		public static Image Controllers => Resources.GameController;

		public static Image Hotkeys => Resources.HotKeys;

		public static Image Display => Resources.TvIcon;

		public static Image Sound => Resources.Audio;

		public static Image Paths => Resources.CopyFolder;

		public static Image Messages => Resources.MessageConfig;

		public static Image Autofire => Resources.Lightning;

		public static Image SaveConfig => Resources.Save;

		public static Image LoadConfig => Resources.LoadConfig;

		public static Image RamWatch => Resources.Watch;

		public static Image RamSearch => Resources.Search;

		public static Image LuaConsole => Resources.TextDoc;

		public static Image TAStudio => Resources.TAStudio;

		public static Image HexEditor => Resources.Poke;

		public static Image Debugger => FeatherIcons.Image("code");

		public static Image OnlineHelp => Resources.Help;

		public static Image About => Resources.ChimeraSmall;

		public static Image CopyVersionInfo => Resources.Duplicate;

		public static Image ReportIssue => FeatherIcons.Image("alert-circle");

		// ---- the gaps, filled with something the frontend already had ------

		public static Image NewProject => Resources.NewFile;

		public static Image OpenProject => Resources.OpenFile;

		public static Image RecentProjects => Resources.Recent;

		public static Image SaveProject => Resources.Save;

		public static Image SaveProjectAs => Resources.SaveAs;

		/// <summary>A backup is a second copy of the project, which is what this says.</summary>
		public static Image SaveProjectBackup => Resources.Duplicate;

		public static Image ScreenshotAs => Resources.SaveAs;

		public static Image LogWindow => Resources.TextDoc;

		/// <summary>The same picture the key-priority status bar icon has used for years.</summary>
		public static Image KeyPriority => Resources.Both;

		public static Image SpeedSkip => Resources.Clock;

		public static Image SaveConfigAs => Resources.SaveAs;

		public static Image LoadConfigFrom => Resources.OpenFile;

		/// <summary>A macro is a recorded run of inputs.</summary>
		public static Image MacroTool => Resources.Record;

		// ---- icons that had no picture of their own ------------------------
		//
		// These had a picture in the frontend already, or were drawn by hand, and
		// are Feather glyphs now (theming/FeatherIcons.cs), so the menu reads as
		// one set. The green dot on McpServerStarted is the one thing Feather has
		// no glyph for: it says the server is running.

		/// <summary>Window size: a screen with a corner being pulled.</summary>
		public static Image WindowSize => FeatherIcons.Image("maximize-2");

		/// <summary>Customize: the sliders of a settings panel.</summary>
		public static Image Customize => FeatherIcons.Image("sliders");

		/// <summary>The batch runner: a list of runs, played through.</summary>
		public static Image BatchRunner => FeatherIcons.Image("play-circle");


		/// <summary>Firmware: a chip.</summary>
		public static Image Firmware => FeatherIcons.Image("cpu");

		/// <summary>The data directory: a disk platter.</summary>
		public static Image DataDirectory => FeatherIcons.Image("hard-drive");

		/// <summary>The theme: a paint palette.</summary>
		public static Image ThemePalette => FeatherIcons.Image("droplet");

		/// <summary>The core manager: a package.</summary>
		public static Image CoreManager => FeatherIcons.Image("package");

		/// <summary>The cache manager: a stack of what is being kept.</summary>
		public static Image CacheManager => FeatherIcons.Image("database");

		/// <summary>The media maker: a disc.</summary>
		public static Image MediaMaker => FeatherIcons.Image("disc");

		/// <summary>Pre-compiled modules: a box, for work already done.</summary>
		public static Image PrecompiledModules => FeatherIcons.Image("box");

		/// <summary>The MCP server (Tools &gt; MCP Server...): a star.</summary>
		public static Image McpServer => FeatherIcons.Image("star");

		/// <summary>
		/// The same with a green dot: the server is started. The menu item is not ticked to say so,
		/// because a tick is drawn where the image is and the two would fight.
		/// </summary>
		public static Image McpServerStarted { get; } = Draw(static g =>
		{
			var star = FeatherIcons.Image("star", 64);
			g.DrawImage(star, 0, 0, 64, 64);
			using SolidBrush started = new(Color.FromArgb(0x4C, 0xB8, 0x6C));
			g.FillEllipse(started, 40, 40, 22, 22);
		});

		/// <summary>Every icon above, by name, for the test that measures them.</summary>
		public static IReadOnlyList<(string Name, Image Image)> All()
			=> typeof(MenuIcons).GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(static p => p.PropertyType == typeof(Image))
				.Select(static p => (p.Name, (Image) p.GetValue(null)!))
				.OrderBy(static x => x.Name, StringComparer.Ordinal)
				.ToList();

		/// <summary>
		/// Renders a glyph at four times the size with antialiasing and brings it
		/// back down, which is the difference between a 16-pixel icon that looks
		/// drawn and one that looks like a mistake.
		/// </summary>
		private static Image Draw(Action<Graphics> glyph)
		{
			const int Big = 64;
			const int Small = 16;
			using Bitmap large = new(Big, Big);
			using (var g = Graphics.FromImage(large))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.Clear(Color.Transparent);
				glyph(g);
			}
			Bitmap small = new(Small, Small);
			using (var g = Graphics.FromImage(small))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.Clear(Color.Transparent);
				g.DrawImage(large, new Rectangle(0, 0, Small, Small), 0, 0, Big, Big, GraphicsUnit.Pixel);
			}
			return small;
		}
	}
}
