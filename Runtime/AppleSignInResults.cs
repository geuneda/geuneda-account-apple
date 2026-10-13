using Geuneda.Core;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple
{
	/// <summary>네이티브 결과를 결과 규약으로 바꿉니다. 플랫폼과 무관한 부분이라 에디터에서 테스트합니다.</summary>
	internal static class AppleSignInResults
	{
		public static Result<PlatformCredential> From(int status, string identityToken, long errorCode, string message)
		{
			switch ((AppleSignInStatus) status)
			{
				case AppleSignInStatus.Succeeded when !string.IsNullOrEmpty(identityToken):
					return Result<PlatformCredential>.Ok(new PlatformCredential(AccountPlatforms.Apple, identityToken));
				case AppleSignInStatus.Succeeded:
					return Result<PlatformCredential>.Fail(AccountResultCodes.PlatformSignInFailed, "Sign in with Apple returned no identity token");
				case AppleSignInStatus.Cancelled:
					return Result<PlatformCredential>.Fail(ResultCodes.Cancelled, "Sign in with Apple was cancelled");
				case AppleSignInStatus.Unsupported:
					return Result<PlatformCredential>.Fail(AccountResultCodes.PlatformSignInFailed, "Sign in with Apple needs iOS 13 or later");
				default:
					return Result<PlatformCredential>.Fail(AccountResultCodes.PlatformSignInFailed, $"Sign in with Apple failed (ASAuthorizationError {errorCode}): {message}");
			}
		}
	}
}
