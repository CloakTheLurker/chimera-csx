namespace Chimera.Client.Common
{
	/// <summary>
	/// What the window says about a greenzone budget the engine says this
	/// computer cannot hold (upstream issue 577). The engine measures; this is
	/// the sentence, in one place, so a project opening and the budget window
	/// say the same thing.
	/// </summary>
	public static class GreenzoneBudget
	{
		/// <summary>
		/// The sentence for a budget the engine says is too big, or null when it
		/// suggests nothing (0: the budget is fine, or the computer will not say
		/// how much memory it has).
		/// </summary>
		public static string WarningText(ulong budgetBytes, ulong suggestedBytes)
			=> suggestedBytes is 0
				? null
				: $"The greenzone budget ({budgetBytes >> 20} MB) is more than this computer can hold: about {suggestedBytes >> 20} MB is all it can spare. Clear Greenzone keeps the smaller size it fits to.";
	}
}
