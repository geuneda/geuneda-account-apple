using System.Threading;
using Cysharp.Threading.Tasks;
using Geuneda.Account.Apple;
using Geuneda.Core;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple.Tests
{
	[TestFixture]
	public class AppleAuthProviderTest
	{
		[Test]
		public void Succeeded_WithToken_IsAppleCredential()
		{
			var result = AppleSignInResults.From(0, "id-token", 0, null);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(AccountPlatforms.Apple, result.Value.Platform);
			Assert.AreEqual("id-token", result.Value.Credential);
		}

		[TestCase(0, null, AccountResultCodes.PlatformSignInFailed)]
		[TestCase(1, null, ResultCodes.Cancelled)]
		[TestCase(2, null, AccountResultCodes.PlatformSignInFailed)]
		[TestCase(3, null, AccountResultCodes.PlatformSignInFailed)]
		public void OtherStatuses_MapToCodes(int status, string token, int expected)
		{
			Assert.AreEqual(expected, AppleSignInResults.From(status, token, 1000, "error").Code);
		}

		[Test]
		public void InEditor_IsUnavailable_AndSignOutIsNoOp()
		{
			var provider = new AppleAuthProvider();

			var result = provider.SignInAsync(CancellationToken.None);
			var signOut = provider.SignOutAsync(CancellationToken.None);

			Assert.AreEqual(AccountPlatforms.Apple, provider.Platform);
			Assert.IsTrue(result.Status.IsCompleted());
			Assert.AreEqual(AccountResultCodes.PlatformSignInFailed, result.GetAwaiter().GetResult().Code);
			Assert.IsTrue(signOut.Status.IsCompleted());
		}

		[Test]
		public void AlreadyCancelled_IsCancelled()
		{
			var cancelled = new CancellationTokenSource();
			cancelled.Cancel();

			Assert.AreEqual(ResultCodes.Cancelled, new AppleAuthProvider().SignInAsync(cancelled.Token).GetAwaiter().GetResult().Code);
		}
	}
}
