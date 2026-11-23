using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Chimera.Common;

namespace Chimera.Client.GUI.Properties
{
	internal static class Resources
	{
		private static Stream EmbeddedResourceStream(string embedPath)
			=> typeof(Resources).Assembly.EmbeddedResourceStream("Chimera.Client.GUI.images." + embedPath);

		/// <param name="filename">Dir separator is '<c>.</c>'. Filename is relative to <c>&lt;NS>/images</c> and omits <c>.png</c> extension.</param>
		private static Bitmap ReadEmbeddedBitmap(string filename) => new Bitmap(EmbeddedResourceStream($"{filename}.png"));

		/// <param name="filename">Dir separator is '<c>.</c>'. Filename is relative to <c>&lt;NS>/images</c> and omits <c>.ico</c> extension.</param>
		private static Icon ReadEmbeddedIcon(string filename) => new Icon(EmbeddedResourceStream($"{filename}.ico"));

		/// <summary>
		/// The command icons are Feather's (https://feathericons.com, MIT), drawn
		/// from <c>images/feather</c> by <see cref="FeatherIcons"/>. The handful that
		/// are not are the ones a set of line drawings cannot say: Chimera's own
		/// logo, the coloured status lights and arrows the window has always had,
		/// and the controller picture.
		/// </summary>
		internal static readonly Bitmap Add = FeatherIcons.Image("plus");
		internal static readonly Bitmap AddEdit = FeatherIcons.Image("edit-3");
		internal static readonly Bitmap AddWatch = FeatherIcons.Image("eye");
		internal static readonly Bitmap ArrowBlackDown = FeatherIcons.Image("chevron-down");
		internal static readonly Bitmap Audio = FeatherIcons.Image("volume-2");
		internal static readonly Bitmap AudioMuted = FeatherIcons.Image("volume-x");
		internal static readonly Bitmap AutoSearch = FeatherIcons.Image("search");
		internal static readonly Bitmap Avi = FeatherIcons.Image("film");
		internal static readonly Bitmap Back = FeatherIcons.Image("arrow-left");
		internal static readonly Bitmap BackMore = FeatherIcons.Image("chevrons-left");
		internal static readonly Bitmap Blank = ReadEmbeddedBitmap("Blank");
		internal static readonly Lazy<Cursor> BlankCursor = new(static () => new(EmbeddedResourceStream("BlankCursor.cur")));
		internal static readonly Bitmap BlueDown = ReadEmbeddedBitmap("BlueDown");
		internal static readonly Bitmap BlueUp = ReadEmbeddedBitmap("BlueUp");
		internal static readonly Bitmap Both = FeatherIcons.Image("columns");
		internal static readonly Icon BugIcon = FeatherIcons.Icon("code");
		internal static readonly Bitmap Bug = FeatherIcons.Image("alert-circle");
		internal static readonly Bitmap Camera = FeatherIcons.Image("camera");
		internal static readonly Bitmap Cheat = FeatherIcons.Image("lock");
		internal static readonly Icon CheatIcon = FeatherIcons.Icon("lock");
		internal static readonly Bitmap Checkbox = FeatherIcons.Image("check-square");
		internal static readonly Bitmap Circle = FeatherIcons.Image("circle");
		internal static readonly Bitmap ClearConsole = FeatherIcons.Image("trash-2");
		internal static readonly Bitmap Close = FeatherIcons.Image("x");
		internal static readonly Icon CommandWindow = FeatherIcons.Icon("terminal");
		internal static readonly Bitmap Connect16X16 = ReadEmbeddedBitmap("connect_16x16");
		internal static readonly Bitmap CopyFolder = FeatherIcons.Image("copy");
		internal static readonly Bitmap Chimera = ReadEmbeddedBitmap("chimera");
		internal static readonly Bitmap ChimeraSmall = ReadEmbeddedBitmap("ChimeraSmall");
		internal static readonly Bitmap Cross = FeatherIcons.Image("x");
		internal static readonly Bitmap Cut = FeatherIcons.Image("scissors");
		internal static readonly Bitmap Debugger = FeatherIcons.Image("code");
		internal static readonly Bitmap Delete = FeatherIcons.Image("trash-2");
		internal static readonly Bitmap Duplicate = FeatherIcons.Image("copy");
		internal static readonly Bitmap ENE = ReadEmbeddedBitmap("ENE");
		internal static readonly Bitmap Erase = FeatherIcons.Image("x-circle");
		internal static readonly Bitmap ESE = ReadEmbeddedBitmap("ESE");
		internal static readonly Bitmap ExclamationRed = FeatherIcons.Image("alert-circle", 16, Color.FromArgb(0xC0, 0x39, 0x2B));
		internal static readonly Bitmap FastForward = FeatherIcons.Image("fast-forward");
		internal static readonly Bitmap Find = FeatherIcons.Image("search");
		internal static readonly Bitmap Forward = FeatherIcons.Image("arrow-right");
		internal static readonly Bitmap Freeze = FeatherIcons.Image("lock");
		internal static readonly Bitmap Fullscreen = FeatherIcons.Image("maximize");
		internal static readonly Icon GameControllerIcon = ReadEmbeddedIcon("GameController");
		internal static readonly Bitmap GameController = ReadEmbeddedBitmap("GameController");
		internal static readonly Bitmap GreenCheck = FeatherIcons.Image("check-circle", 16, Color.FromArgb(0x2E, 0x7D, 0x32));
		internal static readonly Bitmap Help = FeatherIcons.Image("help-circle");
		internal static readonly Icon HotKeysIcon = FeatherIcons.Icon("command");
		internal static readonly Bitmap HotKeys = FeatherIcons.Image("command");
		internal static readonly Bitmap InsertSeparator = FeatherIcons.Image("more-horizontal");
		internal static readonly Bitmap JumpTo = FeatherIcons.Image("crosshair");
		internal static readonly Bitmap KitchenSink = ReadEmbeddedBitmap("kitchensink");
		internal static readonly Icon LightningIcon = FeatherIcons.Icon("zap");
		internal static readonly Bitmap Lightning = FeatherIcons.Image("zap");
		internal static readonly Bitmap LightOff = ReadEmbeddedBitmap("LightOff");
		internal static readonly Bitmap LightOn = ReadEmbeddedBitmap("LightOn");
		internal static readonly Bitmap LoadConfig = FeatherIcons.Image("upload");
		internal static readonly Icon Logo = ReadEmbeddedIcon("logo");
		internal static readonly Bitmap LuaPictureBox = FeatherIcons.Image("image");
		internal static readonly Bitmap MessageConfig = FeatherIcons.Image("message-square");
		internal static readonly Icon MonitorIcon = FeatherIcons.Icon("monitor");
		internal static readonly Bitmap Monitor = ReadEmbeddedBitmap("monitor");
		internal static readonly Bitmap MoveBottom = FeatherIcons.Image("chevrons-down");
		internal static readonly Bitmap MoveDown = FeatherIcons.Image("arrow-down");
		internal static readonly Bitmap MoveLeft = FeatherIcons.Image("arrow-left");
		internal static readonly Bitmap MoveRight = FeatherIcons.Image("arrow-right");
		internal static readonly Bitmap MoveTop = FeatherIcons.Image("chevrons-up");
		internal static readonly Bitmap MoveUp = FeatherIcons.Image("arrow-up");
		internal static readonly Icon MsgBoxIcon = FeatherIcons.Icon("info");
		internal static readonly Bitmap NE = ReadEmbeddedBitmap("NE");
		internal static readonly Bitmap NewFile = FeatherIcons.Image("file-plus");
		internal static readonly Bitmap NNE = ReadEmbeddedBitmap("NNE");
		internal static readonly Bitmap NNW = ReadEmbeddedBitmap("NNW");
		internal static readonly Bitmap NoConnect16X16 = ReadEmbeddedBitmap("noconnect_16x16");
		internal static readonly Bitmap NW = ReadEmbeddedBitmap("NW");
		internal static readonly Bitmap OpenFile = FeatherIcons.Image("folder");
		internal static readonly Bitmap Paste = FeatherIcons.Image("clipboard");
		internal static readonly Bitmap Pause = FeatherIcons.Image("pause");
		internal static readonly Bitmap LuaRunning = ReadEmbeddedBitmap("arrow_green");
		internal static readonly Bitmap Pencil = FeatherIcons.Image("edit-2");
		internal static readonly Bitmap Clock = FeatherIcons.Image("clock");
		internal static readonly Bitmap Play = FeatherIcons.Image("play");
		internal static readonly Bitmap Placeholder = ReadEmbeddedBitmap("placeholder_bitmap");
		internal static readonly Icon PokeIcon = FeatherIcons.Icon("edit-2");
		internal static readonly Bitmap Poke = FeatherIcons.Image("edit-2");
		internal static readonly Bitmap Previous = FeatherIcons.Image("skip-back");
		internal static readonly Bitmap ReadOnly = FeatherIcons.Image("lock");
		internal static readonly Bitmap Reboot = FeatherIcons.Image("refresh-cw");
		internal static readonly Bitmap Recent = FeatherIcons.Image("clock");
		internal static readonly Bitmap Record = FeatherIcons.Image("disc");
		internal static readonly Bitmap Redo = FeatherIcons.Image("rotate-cw");
		internal static readonly Bitmap Refresh = FeatherIcons.Image("refresh-cw");
		internal static readonly Bitmap Restart = FeatherIcons.Image("rotate-ccw");
		internal static readonly Bitmap RewindRecord = FeatherIcons.Image("rewind");
		internal static readonly Bitmap Save = FeatherIcons.Image("save");
		internal static readonly Bitmap SaveAs = FeatherIcons.Image("save");
		internal static readonly Bitmap Scan = FeatherIcons.Image("crosshair", 32);
		internal static readonly Bitmap ScrollTo = FeatherIcons.Image("crosshair");
		internal static readonly Bitmap SE = ReadEmbeddedBitmap("SE");
		internal static readonly Bitmap Search = FeatherIcons.Image("search");
		internal static readonly Icon SearchIcon = FeatherIcons.Icon("search");
		internal static readonly Bitmap Split = FeatherIcons.Image("columns");
		internal static readonly Bitmap Square = FeatherIcons.Image("square");
		internal static readonly Bitmap SSE = ReadEmbeddedBitmap("SSE");
		internal static readonly Bitmap SSW = ReadEmbeddedBitmap("SSW");
		internal static readonly Bitmap Stop = FeatherIcons.Image("square");
		internal static readonly Bitmap SW = ReadEmbeddedBitmap("SW");
		internal static readonly Icon TAStudioIcon = FeatherIcons.Icon("activity");
		internal static readonly Bitmap TAStudio = FeatherIcons.Image("activity");
		internal static readonly Bitmap TextDoc = FeatherIcons.Image("file-text", 32);
		internal static readonly Icon TextDocIcon = FeatherIcons.Icon("file-text");
		internal static readonly Bitmap Triangle = FeatherIcons.Image("triangle");
		internal static readonly Bitmap TruncateFromFile = FeatherIcons.Image("scissors");
		internal static readonly Bitmap TvIcon = FeatherIcons.Image("monitor");
		internal static readonly Bitmap Undo = FeatherIcons.Image("rotate-ccw");
		internal static readonly Bitmap Unfreeze = FeatherIcons.Image("unlock");
		internal static readonly Bitmap UpdateBranch = FeatherIcons.Image("git-branch");
		internal static readonly Bitmap UpdateWithText = FeatherIcons.Image("git-merge");
		internal static readonly Bitmap Watch = FeatherIcons.Image("eye");
		internal static readonly Icon WatchIcon = FeatherIcons.Icon("eye");
		internal static readonly Bitmap WNW = ReadEmbeddedBitmap("WNW");
		internal static readonly Bitmap WSW = ReadEmbeddedBitmap("WSW");
		internal static readonly Bitmap YellowDown = ReadEmbeddedBitmap("YellowDown");
		internal static readonly Bitmap YellowLeft = ReadEmbeddedBitmap("YellowLeft");
		internal static readonly Bitmap YellowRight = ReadEmbeddedBitmap("YellowRight");
		internal static readonly Bitmap YellowUp = ReadEmbeddedBitmap("YellowUp");
	}
}
