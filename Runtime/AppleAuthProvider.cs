using System.Threading;
using Cysharp.Threading.Tasks;
using Geuneda.Core;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple
{
	/// <summary>
	/// Sign in with Apple 제공자입니다. iOS 13 이상에서 애플 로그인 시트를 띄워 id 토큰을 받고, 서버의 <c>AppleIdTokenVerifier</c> 가 검증합니다.
	/// <c>AuthServiceOptions.Providers</c> 에 등록합니다.
	/// </summary>
	/// <remarks>
	/// <para>
	/// 이름·이메일은 요청하지 않습니다(서버는 <c>sub</c> 로 계정을 찾음). 사용자가 시트를 닫으면 <c>ResultCodes.Cancelled</c>,
	/// 그 밖의 실패는 <see cref="AccountResultCodes.PlatformSignInFailed"/> 입니다. iOS 가 아니거나 iOS 12 이하면 실패로 돌려줍니다.
	/// </para>
	/// <para>
	/// 애플은 앱에서 로그아웃하는 API 가 없어 <see cref="SignOutAsync"/> 는 아무것도 하지 않습니다(연동 해제는 사용자가 설정 앱에서).
	/// iOS 빌드에는 에디터 훅이 Sign in with Apple capability 를 넣으므로, App ID 에도 같은 capability 를 켜야 서명됩니다.
	/// </para>
	/// </remarks>
	public sealed class AppleAuthProvider : IAuthProvider
	{
		public string Platform => AccountPlatforms.Apple;

		public UniTask<Result<PlatformCredential>> SignInAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return UniTask.FromResult(Result<PlatformCredential>.Fail(ResultCodes.Cancelled));
			}

#if UNITY_IOS && !UNITY_EDITOR
			return new AppleSignInRequest().StartAsync(cancellationToken);
#else
			return UniTask.FromResult(Result<PlatformCredential>.Fail(AccountResultCodes.PlatformSignInFailed, "Sign in with Apple is only available on iOS"));
#endif
		}

		public UniTask SignOutAsync(CancellationToken cancellationToken)
		{
			return UniTask.CompletedTask;
		}
	}
}
