#nullable enable

namespace Chimera.Emulation.Common
{
	/// <summary>
	/// A core whose memory-callback values can be replaced (issue 534): bit
	/// (1 &lt;&lt; kind) for each kind it lets a callback change, 1 a read and
	/// 2 a write. An instruction has no value to change and is never a bit.
	/// A core that does not implement this only reports what it sees.
	/// </summary>
	public interface IMemoryCallbackReplaceable
	{
		int ReplaceableMemoryKinds { get; }
	}
}
