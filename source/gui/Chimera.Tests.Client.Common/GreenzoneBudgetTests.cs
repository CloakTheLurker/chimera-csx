using Chimera.Client.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chimera.Tests.Client.Common
{
	/// <summary>
	/// The sentence a person reads when the greenzone budget is more than the
	/// computer can hold (upstream issue 577). The engine measures; this is the
	/// one place the wording lives, so a project opening and the budget window
	/// say the same thing.
	/// </summary>
	[TestClass]
	public class GreenzoneBudgetTests
	{
		[TestMethod]
		public void ATooBigBudgetSaysBothFigures()
		{
			var text = GreenzoneBudget.WarningText(20UL * 1024 * 1024 * 1024, 15UL * 1024 * 1024 * 1024);
			StringAssert.Contains(text, "20480 MB", "the budget set is named");
			StringAssert.Contains(text, "15360 MB", "the size the computer can hold is named");
			StringAssert.Contains(text, "Clear Greenzone", "and what a clear does with it");
		}

		[TestMethod]
		public void ABudgetThatFitsSaysNothing()
			=> Assert.IsNull(GreenzoneBudget.WarningText(4UL * 1024 * 1024 * 1024, 0));
	}
}
