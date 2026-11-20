using Chimera.Emulation.Common;

namespace Chimera.Client.GUI
{
	/// <summary>
	/// The idle screen as an untuned television: grey snow, a band rolling down
	/// it, and the scan lines of a set nobody has found a channel on. Dim on
	/// purpose - the idle screen is what lights a room when nothing is
	/// happening, and the eye's rule holds for the snow too.
	///
	/// The snow is drawn in blocks rather than a pixel at a time: the display
	/// smooths the frame up to the window's size, and single pixels of noise
	/// would average into flat grey. The picture changes twelve times a second
	/// and is held in between, which is what an untuned set does and what keeps
	/// an idle machine idle.
	/// </summary>
	public sealed class IdleStaticVideo : IVideoProvider
	{
		private const int Width = 480;

		private const int Height = 360;

		/// <summary>the side of one flake of snow, in screen pixels</summary>
		private const int Flake = 4;

		/// <summary>the brightest a flake gets, out of 255</summary>
		private const int Brightest = 70;

		/// <summary>frames one picture is held for at 60 a second: twelve pictures a second</summary>
		private const int Hold = 5;

		/// <summary>how many rows the rolling band covers</summary>
		private const int Band = 90;

		private readonly int[] _buffer = new int[Width * Height];

		/// <summary>the noise: a small xorshift, which is all snow needs</summary>
		private uint _noise = 0x5713EED5u;

		private long _frame;

		private long _shown = -1;

		public int VirtualWidth => Width;

		public int VirtualHeight => Height;

		public int BufferWidth => Width;

		public int BufferHeight => Height;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 60;

		public int VsyncDenominator => 1;

		public int[] GetVideoBuffer()
		{
			var picture = _frame / Hold;
			_frame++;
			if (picture != _shown)
			{
				_shown = picture;
				Draw(picture);
			}
			return _buffer;
		}

		private void Draw(long picture)
		{
			// seventy-five rows a second: the band takes about six seconds to cross
			var band = (int) ((picture * (Height + Band) / 72) % (Height + Band)) - Band;

			for (var y = 0; y < Height; y += Flake)
			{
				// the band is brighter than the snow around it and fades as it
				// passes, the way an old set's picture rolls
				var distance = Math.Abs(y - band);
				var gain = distance < Band ? 1.4f - (distance * 0.004f) : 1f;
				// every other line is dimmer: the scan lines of the picture
				var scan = (y / Flake) % 2 == 0 ? 1f : 0.72f;
				for (var x = 0; x < Width; x += Flake)
				{
					_noise ^= _noise << 13;
					_noise ^= _noise >> 17;
					_noise ^= _noise << 5;
					var level = (int) ((_noise & 0xFF) * (Brightest / 255f) * gain * scan);
					if (level > 255) level = 255;
					var pixel = unchecked((int) 0xFF000000) | (level << 16) | (level << 8) | level;
					for (var dy = 0; dy < Flake; dy++)
					{
						var row = ((y + dy) * Width) + x;
						for (var dx = 0; dx < Flake; dx++) _buffer[row + dx] = pixel;
					}
				}
			}
		}
	}
}
