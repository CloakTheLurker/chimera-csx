using System.Collections.Generic;

using Chimera.Client.Common;

namespace Chimera.Tests.Client.Common
{
	/// <summary>
	/// The scripts' userdata as a branch keeps it (issue 534): one text, each
	/// value keeping its kind, a name or value holding anything, and "" (a kept,
	/// empty bag) different from null (kept none).
	/// </summary>
	[TestClass]
	public class UserDataTextTests
	{
		[TestMethod]
		public void AValueKeepsItsKindAndItsBytes()
		{
			List<KeyValuePair<string, object>> bag = new()
			{
				new("hits", 5),
				new("done", true),
				new("name with space", "a b\nc%"),
				new("half", -0.0),
				new("big", 18446744073709551615UL),
			};

			var text = UserDataText.Serialize(bag);
			var back = UserDataText.Parse(text);

			Assert.AreEqual(5, back.Count);
			Assert.AreEqual(5L, back.Find(p => p.Key == "hits").Value);
			Assert.AreEqual(true, back.Find(p => p.Key == "done").Value);
			Assert.AreEqual("a b\nc%", back.Find(p => p.Key == "name with space").Value);
			Assert.AreEqual(-0.0, back.Find(p => p.Key == "half").Value);
			Assert.AreEqual(18446744073709551615UL, back.Find(p => p.Key == "big").Value);
			Assert.IsTrue(1.0 / (double)back.Find(p => p.Key == "half").Value < 0); // -0.0 stayed negative
		}

		[TestMethod]
		public void FractionsThatAreNotNumbersSurvive()
		{
			List<KeyValuePair<string, object>> bag = new()
			{
				new("nan", double.NaN),
				new("inf", double.PositiveInfinity),
				new("minus", double.NegativeInfinity),
			};
			var back = UserDataText.Parse(UserDataText.Serialize(bag));
			Assert.IsTrue(double.IsNaN((double)back.Find(p => p.Key == "nan").Value));
			Assert.IsTrue(double.IsPositiveInfinity((double)back.Find(p => p.Key == "inf").Value));
			Assert.IsTrue(double.IsNegativeInfinity((double)back.Find(p => p.Key == "minus").Value));
		}

		[TestMethod]
		public void AnEmptyTextIsAnEmptyBagAndNullIsNotText()
		{
			Assert.AreEqual(0, UserDataText.Parse("").Count);
			Assert.AreEqual(0, UserDataText.Parse(null).Count);
			Assert.AreEqual("", UserDataText.Serialize(new List<KeyValuePair<string, object>>()));
		}
	}
}
