using System;
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

			var result = provider.SignInAsync("nonce", CancellationToken.None);
			var signOut = provider.SignOutAsync(CancellationToken.None);

			Assert.AreEqual(AccountPlatforms.Apple, provider.Platform);
			Assert.IsTrue(provider.RequiresNonce);
			Assert.IsTrue(result.Status.IsCompleted());
			Assert.AreEqual(AccountResultCodes.PlatformSignInFailed, result.GetAwaiter().GetResult().Code);
			Assert.IsTrue(signOut.Status.IsCompleted());
		}

		[Test]
		public void AlreadyCancelled_IsCancelled()
		{
			var cancelled = new CancellationTokenSource();
			cancelled.Cancel();

			Assert.AreEqual(ResultCodes.Cancelled, new AppleAuthProvider().SignInAsync("nonce", cancelled.Token).GetAwaiter().GetResult().Code);
		}

		[TestCase(null)]
		[TestCase("")]
		public void MissingNonce_Throws(string nonce)
		{
			Assert.Throws<ArgumentException>(() => new AppleAuthProvider().SignInAsync(nonce, CancellationToken.None));
		}

		[TestCase("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
		// 서버 테스트(IdTokenVerifierTest)의 OneTimeValues.Hash 와 같은 값이어야 한다
		[TestCase("raw-nonce-value", "90d1bd536f17b7ddb159e27bb5716ae0db8a41b69dc20ffe276681196112b496")]
		public void RequestNonce_IsLowercaseHexSha256OfTheServerNonce(string nonce, string expected)
		{
			Assert.AreEqual(expected, AppleNonce.Hash(nonce));
		}
	}
}
