#nullable enable

using System;

namespace Chimera.Client.Common
{
	/// <summary>
	/// A branch was loaded and the machine is on its frame. TAStudio raises it;
	/// every Lua engine (one per Lua Console) listens while it lives, and calls
	/// the scripts' event.onloadstate functions with the branch's name.
	///
	/// It is raised for a branch load only, as issue 534 asks: not for a seek,
	/// a rewind, or memorysavestate.loadcorestate.
	/// </summary>
	public static class BranchLoadEvents
	{
		public static event Action<string>? Loaded;

		public static void Raise(string branchName) => Loaded?.Invoke(branchName ?? "");
	}
}
