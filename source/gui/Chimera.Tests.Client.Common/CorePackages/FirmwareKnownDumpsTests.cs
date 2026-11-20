using System.IO;
using System.Linq;

using Chimera.Client.Common;
using Chimera.Emulation.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace Chimera.Tests.Client.Common.CorePackages
{
	/// <summary>
	/// One boot ROM is dumped several ways: a Game Boy Color's has a 256-byte gap
	/// in the middle that some dumpers fill and others leave out, so the same ROM
	/// hashes differently. A package names the dump it was verified with and may
	/// list the others it knows; any of them answers the requirement. A package
	/// that names only the first still reads, because the extra key is new.
	/// </summary>
	[TestClass]
	public class FirmwareKnownDumpsTests
	{
		private const string Verified = "1293D68BF9643BC4F36954C1E80E38F39864528D";
		private const string Other = "DF5A0D2D49DE38FBD31CC2AAB8E62C8550E655C0";

		private static string FileOf(int size, byte seed)
		{
			var path = Path.Combine(Path.GetTempPath(), $"chimera-fw-dumps-{Path.GetRandomFileName()}");
			var bytes = new byte[size];
			for (var i = 0; i < size; i++) bytes[i] = (byte) (i + seed);
			File.WriteAllBytes(path, bytes);
			return path;
		}

		private static CoreFirmwareDecl Decl(params string[] extras)
			=> new() { Id = "gbcBoot", Display = "Game Boy Color boot ROM", Size = 2048, Sha1 = Verified, Sha1s = extras.ToList() };

		[TestMethod]
		public void ThePackageFormatReadsOneDumpOrSeveral()
		{
			var one = JsonConvert.DeserializeObject<CoreFirmwareDecl>(
				$"{{\"id\":\"gbcBoot\",\"size\":2304,\"sha1\":\"{Verified}\"}}")!;
			CollectionAssert.AreEqual(new[] { Verified }, one.KnownSha1s.ToArray(), "one dump, as every package before this wrote it");

			var several = JsonConvert.DeserializeObject<CoreFirmwareDecl>(
				$"{{\"id\":\"gbcBoot\",\"size\":2304,\"sha1\":\"{Verified}\",\"sha1s\":[\"{Other}\",\"{Verified.ToLowerInvariant()}\"]}}")!;
			CollectionAssert.AreEqual(new[] { Verified, Other }, several.KnownSha1s.ToArray(),
				"the verified dump first, no repeat of it, whatever case the package wrote");
		}

		[TestMethod]
		public void AFileMatchingAnyKnownDumpIsGood()
		{
			var path = FileOf(2048, seed: 3);
			var sha1 = CoreFirmwareStore.Sha1Of(File.ReadAllBytes(path));
			try
			{
				Config config = new();
				CoreFirmwareStore.SetPath(config, "ares", "gbcBoot", path);
				var entry = CoreFirmwareStore.Describe(config, "ares", Decl(sha1));
				Assert.AreEqual(CoreFirmwareState.Good, entry.State, "the second dump the package knows was not accepted");
				CollectionAssert.AreEqual(new[] { Verified, sha1 }, entry.ExpectedSha1.ToArray(), "both dumps are named as expected");
			}
			finally
			{
				File.Delete(path);
			}
		}

		[TestMethod]
		public void AFileChosenAsTheSecondDumpIsStillTheChoice()
		{
			var path = FileOf(2048, seed: 9);
			var sha1 = CoreFirmwareStore.Sha1Of(File.ReadAllBytes(path));
			try
			{
				Config config = new();
				// remembered under the dump it turned out to be, as a project pin does
				CoreFirmwareStore.Remember(config, "ares", new CoreFirmwareDecl { Id = "gbcBoot", Sha1 = sha1 }, path);
				Assert.AreEqual(path, CoreFirmwareStore.GetPath(config, "ares", Decl(sha1)), "the file chosen for the second dump was forgotten");
			}
			finally
			{
				File.Delete(path);
			}
		}
	}
}
