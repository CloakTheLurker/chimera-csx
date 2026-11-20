using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chimera.Client.GUI
{
	/// <summary>
	/// The lines the Log Window shows. It belongs to the frontend rather than to
	/// the window, which is only a view of it: the log can run in the background,
	/// kept from the moment Chimera starts, so opening the window later shows the
	/// whole session and closing it throws nothing away. While it records it
	/// holds the console and writes every line on to it as well, then hands it
	/// back when it stops, so a headless run's output stays what it was - scripts,
	/// and the witness, read that.
	/// </summary>
	public sealed class LogBuffer
	{
		private readonly List<string> _lines = new();

		private readonly LogWriter _writer = new();

		private readonly object _lock = new();

		/// <summary>the console outside, kept while this holds it</summary>
		private TextWriter _console;

		/// <summary>how many Log Windows are open on this buffer</summary>
		private int _views;

		/// <summary>Raised from whatever thread wrote the line, so a window must
		/// post its update rather than draw from here.</summary>
		public event Action Changed;

		/// <summary>Whether lines are being kept: from startup when the log runs in
		/// the background, otherwise only while a Log Window is open.</summary>
		public bool Capturing { get; private set; }

		/// <summary>Whether the log is kept while no Log Window is open.</summary>
		public bool Background { get; private set; }

		public int Count
		{
			get
			{
				lock (_lock) return _lines.Count;
			}
		}

		public string this[int index]
		{
			get
			{
				lock (_lock) return _lines[index];
			}
		}

		/// <summary>Turns background recording on or off. Turning it off while a
		/// window is open keeps recording until that window closes.</summary>
		public void SetBackground(bool on)
		{
			Background = on;
			if (on) Start();
			else if (_views is 0) Stop();
		}

		public void ViewOpened()
		{
			_views++;
			Start();
		}

		public void ViewClosed()
		{
			if (_views > 0) _views--;
			if (_views is 0 && !Background) Stop();
		}

		public void Start()
		{
			if (Capturing) return;
			Capturing = true;
			_console = Console.Out;
			_writer.Emit = Append;
			// what is kept is also written on to the console this took, so a
			// headless run's output still reaches whoever started it
			_writer.Tee = _console;
			Console.SetOut(_writer);
		}

		public void Stop()
		{
			if (!Capturing) return;
			Capturing = false;
			_writer.Emit = null;
			_writer.Tee = null;
			Console.SetOut(_console ?? new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
			_console = null;
		}

		/// <summary>A line from somewhere the console does not reach: an on-screen
		/// message, which is as much a record as the console's own.</summary>
		public void Append(string message)
		{
			// nothing is kept while the log is not running: with background
			// recording off, that is everything written while no window is open
			if (!Capturing) return;
			var added = false;
			foreach (var line in message.Split('\n'))
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				lock (_lock) _lines.Add(line.TrimEnd('\r'));
				added = true;
			}
			if (added) Changed?.Invoke();
		}

		public void Clear()
		{
			lock (_lock) _lines.Clear();
			Changed?.Invoke();
		}

		/// <summary>Shows a report in place of the log, as a dialog with nothing of
		/// its own does.</summary>
		public void ShowReport(string report)
		{
			lock (_lock)
			{
				_lines.Clear();
				foreach (var line in report.Split('\n'))
				{
					if (string.IsNullOrWhiteSpace(line)) continue;
					_lines.Add(line.TrimEnd('\r'));
				}
			}
			Changed?.Invoke();
		}

		private sealed class LogWriter : TextWriter
		{
			public Action<string> Emit;

			/// <summary>where the line also goes: the console this writer replaced</summary>
			public TextWriter Tee;

			public override void Write(char[] buffer, int offset, int count)
			{
				Emit?.Invoke(new string(buffer, offset, count));
				if (Tee is null) return;
				Tee.Write(buffer, offset, count);
				Tee.Flush();
			}

			public override void Flush() => Tee?.Flush();

			public override Encoding Encoding => Encoding.Unicode;
		}
	}
}
