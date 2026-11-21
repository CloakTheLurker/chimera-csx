using System;
using System.IO;

using Chimera.Client.Common;
using Chimera.Emulation.Common;

namespace Chimera.Client.GUI
{
	public partial class TAStudio : IToolForm
	{
		[RequiredService]
		public IEmulator Emulator { get; private set; }

		[RequiredService]
		public IStatable StatableEmulator { get; private set; }

		[RequiredService]
		public IVideoProvider VideoProvider { get; private set; }

		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		private int _lastRefresh;
		private bool _doPause;
		private int _lastRecordAction = -1;

		// A seek that replays for a long time says how long, asks first, and can be
		// stopped from its own bar (upstream issue 595). The speed is the one the
		// last seek ran at, in frames a second: the window's own measurement, as
		// this engine has no measured-fps export.
		private readonly System.Diagnostics.Stopwatch _seekClock = new();
		private int _seekFramesWatched;
		private double _seekFramesPerSecond;
		private readonly System.Windows.Forms.ToolStripButton _seekCancel = new();

		private void UpdateProgressBar()
		{
			if (SeekingTo != -1)
			{
				int diff = Emulator.Frame - _seekStartFrame;
				int unit = SeekingTo - _seekStartFrame;
				double progress = 0;

				if (diff != 0 && unit != 0)
				{
					progress = (double)100d / unit * diff;
				}

				if (progress < 0)
				{
					progress = 0;
				}
				else if (progress > 100)
				{
					progress = 100;
				}

				ProgressBar.Value = (int)progress;

				/* how fast this replay is going - the frames played over the
				 * seconds they took - and what is left of it, kept for the next
				 * seek's estimate (upstream issue 595) */
				_seekFramesWatched++;
				if (_seekClock.Elapsed.TotalSeconds >= 0.5)
				{
					_seekFramesPerSecond = _seekFramesWatched / _seekClock.Elapsed.TotalSeconds;
				}
				var left = SeekingTo - Emulator.Frame;
				var about = _seekFramesPerSecond > 0
					? $", about {Duration(Math.Max(0, left) / _seekFramesPerSecond)} left"
					: "";
				MessageStatusLabel.Text = $"Seeking... {diff:n0} of {unit:n0} frames{about}";
				_seekCancel.Visible = true;
			}
			else
			{
				ProgressBar.Visible = false;
				MessageStatusLabel.Text = "";
				_seekCancel.Visible = false;
			}
		}

		/// <summary>"40 s", "2 min 5 s" - a seek's estimate (upstream issue 595).</summary>
		private static string Duration(double seconds)
		{
			if (seconds < 60) return $"{Math.Max(0, seconds):F0} s";
			var whole = (int)Math.Round(seconds);
			return $"{whole / 60} min {whole % 60} s";
		}

		protected override void UpdateBefore()
		{
			if (CurrentTasMovie.IsAtEnd() && !CurrentTasMovie.IsRecording())
			{
				// Past the end of the log there is no row to play back, so this frame
				// is AUTHORED here rather than replayed - and what it holds is what
				// the machine is then handed, because MovieSession reads the row back
				// a moment later.
				//
				// A seek turns recording off for its duration (GoToFrame) so that the
				// rows it passes over are replayed instead of being typed over. Out
				// here there are no rows to protect, and writing the autoholds alone
				// would throw away everything actually being pressed - a hand on the
				// pad, or a script's joypad.set - for as long as the movie is being
				// extended. WasRecording is the person's own answer to "am I
				// authoring": when it is set, the extension is written from the same
				// input record mode would have written (MovieIn), so a script and a
				// hand reach the movie the same way (issue #95).
				//
				// Read-only play past the end is untouched: with nobody recording,
				// the autoholds remain the only way to hold a button out here.
				IController extendFrom = WasRecording && MovieSession.MovieIn is not null
					? MovieSession.MovieIn
					: MovieSession.StickySource;
				CurrentTasMovie.RecordFrame(CurrentTasMovie.Emulator.Frame, extendFrom);
			}
		}

		protected override void FastUpdateBefore() => UpdateBefore();

		protected override void UpdateAfter()
		{
			if (!IsHandleCreated || IsDisposed || CurrentTasMovie == null)
			{
				return;
			}

			if (_exiting)
			{
				return;
			}

			bool refreshNeeded = _inputRolls[0].RowCount != CurrentTasMovie.InputLogLength + 1;
			if (Settings.AutoadjustInput)
			{
				//refreshNeeded = AutoAdjustInput();
			}

			CurrentTasMovie.TasSession.UpdateValues(Emulator.Frame, CurrentTasMovie.Branches.Current);
			MaybeFollowCursor();

			if (Settings.AutoPause && SeekingTo == -1)
			{
				if (_doPause && CurrentTasMovie.IsAtEnd()) MainForm.PauseEmulator();
				_doPause = !CurrentTasMovie.IsAtEnd();
			}

			if (!_seekingByEdit)
			{
				_shouldMoveGreenArrow = true;
			}

			FastUpdateAfter();
			RefreshDialog(refreshNeeded, refreshBranches: false);
			if (!refreshNeeded)
			{
				_inputRolls.ForEach(r => r.InvalidateRow(_lastRefresh));
				_inputRolls.ForEach(r => r.InvalidateRow(Emulator.Frame));
				_lastRefresh = Emulator.Frame;
			}
		}

		protected override void FastUpdateAfter()
		{
			if (SeekingTo != -1 && Emulator.Frame >= SeekingTo)
			{
				var reached = _branchAwaitingState;
				_branchAwaitingState = null;
				bool smga = _shouldMoveGreenArrow;
				StopSeeking();
				_shouldMoveGreenArrow = smga;
				// a branch reached by replaying keeps the state it has just run
				// on: its old one was another build's, and without this every
				// load replays again (upstream issue 614). A seek that stopped
				// short of the branch's frame wrote nothing.
				if (reached is not null && Emulator.Frame == reached.Frame)
				{
					BookMarkControl.SaveStateAfterReplay(reached);
				}
			}
			UpdateProgressBar();
			SaveGreenzoneAtSelectedMarker();
		}

		// the frame the selected marker's checkpoint was last taken at, so crossing
		// the marker again is a new checkpoint and standing on it is not
		private int _greenzoneCheckpointFrame = -1;

		/// <summary>
		/// A selected marker is a checkpoint: when the machine reaches its frame, the
		/// project's greenzone is written to its cache - the regenerable half of a
		/// save, without the project - so the states up to there survive a crash, a
		/// core that falls over, or a session that simply ends. Once per arrival:
		/// standing on the frame, or coming back to it, writes again only after the
		/// machine has left it.
		/// </summary>
		private void SaveGreenzoneAtSelectedMarker()
		{
			var marker = MarkerControl.SelectedMarker;
			if (marker is null || Emulator.Frame != marker.Frame)
			{
				_greenzoneCheckpointFrame = -1;
				return;
			}
			if (_greenzoneCheckpointFrame == marker.Frame) return;
			_greenzoneCheckpointFrame = marker.Frame;
			try
			{
				CurrentTasMovie.SaveGreenzoneCache();
				MainForm.AddOnScreenMessage($"Greenzone saved at marker {marker.Frame}");
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
			{
				// the cache is the part that may fail; the run is not. What did not
				// land now is written by the next save
				MainForm.AddOnScreenMessage($"Could not save the greenzone at marker {marker.Frame}");
			}
		}

		public override void Restart()
		{
			if (!IsActive)
			{
				return;
			}

			if (_initializing)
			{
				return;
			}

			// Reboot Core inside a project: the movie in hand IS the project and goes
			// on to the rebooted machine. Stopping it here and starting another is
			// what used to replace the project with a blank one (issue #196); the
			// main window calls ProjectRebooted when the new machine has the movie.
			if (MainForm.ProjectIsRebooting)
			{
				return;
			}

			if (CurrentTasMovie != null)
			{
				bool loadRecent = Game.Hash == CurrentTasMovie.Hash && CurrentTasMovie.Filename == Settings.RecentTas.MostRecent;
				TastudioStopMovie();
				// try to load the most recent movie if it matches the currently loaded movie
				if (loadRecent)
				{
					LoadMostRecentOrStartNew();
				}
				else
				{
					StartNewTasMovie();
				}
			}
		}

		/// <summary>
		/// Ask whether changes should be saved. Returns false if cancelled, else true.
		/// </summary>
		public override bool AskSaveChanges()
		{
			if (_suppressAskSave)
			{
				return true;
			}

			// a reboot of the project's machine loses nothing of the project
			if (MainForm.ProjectIsRebooting) return true;

			if (CurrentTasMovie?.Changes is not true) return true;

			/* Nobody to ask. A script's client.exit() reaches here with a modal
			 * three-way question and no hand to answer it: the box waits on the
			 * desktop forever while the run loop spins beside it, which is what
			 * a "hang on exit" turned out to be. Take the branch that leaves the
			 * project on disk exactly as it was - the unsaved edits are the
			 * script's own, and a script that wanted them kept had the API to
			 * save them - and say so, because a discarded change should never be
			 * silent. */
			if (MainForm.ShutdownIsUnattended)
			{
				Console.Error.WriteLine(
					"[tastudio] closing unattended with unsaved changes; the project on disk is unchanged");
				CurrentTasMovie.ClearChanges();
				return true;
			}

			var shouldSaveResult = DialogController.DoWithTempMute(() => this.ModalMessageBox3(
				caption: "Closing with Unsaved Changes",
				icon: EMsgBoxIcon.Question,
				// the thing being saved is the PROJECT, whatever window you closed
				text: "Save Chimera Project?"));
			if (shouldSaveResult == true)
			{
				TryAgainResult saveResult = this.DoWithTryAgainBox(() => SaveTas(), "Failed to save movie.");
				return saveResult != TryAgainResult.Canceled;
			}
			if (shouldSaveResult is null) return false;
			else CurrentTasMovie.ClearChanges();
			return true;
		}
	}
}
