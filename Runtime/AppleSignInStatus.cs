// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple
{
	/// <summary>네이티브 플러그인(<c>GeunedaAppleSignIn.mm</c>)이 돌려주는 상태입니다. 값은 플러그인과 같아야 합니다.</summary>
	internal enum AppleSignInStatus
	{
		Succeeded = 0,
		Cancelled = 1,
		Failed = 2,
		/// <summary>iOS 13 미만이라 Sign in with Apple 을 쓸 수 없음</summary>
		Unsupported = 3
	}
}
