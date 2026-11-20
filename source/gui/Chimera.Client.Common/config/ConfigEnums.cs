namespace Chimera.Client.Common
{
	public enum ESoundOutputMethod
	{
		LegacyDirectSound, // kept here to handle old configs
		XAudio2, // kept here to handle old configs; output removed, maps to OpenAL
		OpenAL,
		Dummy,
	}

	public enum EDispManagerAR
	{
		None = 0,
		System = 1,
		CustomSize = 2,
		CustomRatio = 3,
	}

	public enum SaveStateType
	{
		Binary,
		Text,
	}

	/// <summary>What the screen shows while no project is open. The numbers are
	/// written to config.ini, so they keep their meaning.</summary>
	public enum EIdleScreen
	{
		/// <summary>nothing of Chimera's own: the null core's frame</summary>
		Off = 0,

		/// <summary>the mark, held still</summary>
		Eye = 1,

		/// <summary>the mark, glancing after the pointer and blinking</summary>
		AnimatedEye = 2,

		/// <summary>television static</summary>
		TvStatic = 3,
	}
}
