#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using Cysharp.Threading.Tasks;
using Geuneda.Core;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple
{
	/// <summary>
	/// 로그인 요청 하나입니다. 네이티브 플러그인에 GCHandle 을 넘기고, 플러그인이 끝나면 그 핸들로 이 요청을 찾아 결과를 넘깁니다(static 상태 없이 콜백을 연결).
	/// 핸들은 네이티브 콜백에서만 풀므로, 먼저 취소로 끝나도 늦게 오는 콜백이 해제된 핸들을 쓰지 않습니다.
	/// 네이티브는 요청마다 콜백을 정확히 한 번 부르고(취소 포함), 콜백과 취소는 모두 메인 스레드에서 실행됩니다.
	/// </summary>
	internal sealed class AppleSignInRequest
	{
		private delegate void CompletionCallback(IntPtr context, int status, string identityToken, long errorCode, string message);

		// 네이티브가 들고 있을 함수 포인터. 수거되지 않게 둔다(바뀌지 않는 값)
		private static readonly CompletionCallback Completed = OnCompleted;

		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.Account.Apple");

		private readonly UniTaskCompletionSource<Result<PlatformCredential>> _completion = new UniTaskCompletionSource<Result<PlatformCredential>>();
		// 네이티브 요청의 GCHandle 값. 네이티브 콜백이 오면 0 으로 바꾼다(그 뒤 같은 값을 다음 요청이 받을 수 있다)
		private IntPtr _context;

		[DllImport("__Internal")]
		private static extern void GeunedaAppleSignIn_Start(IntPtr context, string nonce, CompletionCallback callback);

		[DllImport("__Internal")]
		private static extern void GeunedaAppleSignIn_Cancel(IntPtr context);

		/// <param name="requestNonce">애플 요청에 넣을 nonce(서버 nonce 원문의 SHA-256 16진 소문자). id 토큰의 <c>nonce</c> 클레임이 됩니다.</param>
		public async UniTask<Result<PlatformCredential>> StartAsync(string requestNonce, CancellationToken cancellationToken)
		{
			_context = GCHandle.ToIntPtr(GCHandle.Alloc(this));
			GeunedaAppleSignIn_Start(_context, requestNonce, Completed);

			// 취소는 아무 스레드에서나 올 수 있어 UIKit 을 부르는 취소는 메인 스레드로 넘긴다
			using (cancellationToken.Register(() => MainThreadDispatcher.Post(Cancel)))
			{
				return await _completion.Task;
			}
		}

		private void Cancel()
		{
			// 이미 끝난 요청의 늦은 취소(완료 결과와 취소가 함께 큐에 들어간 경우)는 다음 요청을 건드리지 않게 아무것도 하지 않는다
			if (_context == IntPtr.Zero)
			{
				return;
			}

			// 네이티브가 취소로 콜백해 핸들을 푼다. iOS 16 이상은 시트도 닫고, 그 아래는 시트가 남지만 결과는 여기서 끝낸다
			GeunedaAppleSignIn_Cancel(_context);
			_completion.TrySetResult(Result<PlatformCredential>.Fail(ResultCodes.Cancelled));
		}

		[MonoPInvokeCallback(typeof(CompletionCallback))]
		private static void OnCompleted(IntPtr context, int status, string identityToken, long errorCode, string message)
		{
			// 네이티브 콜백 안에서 예외가 나면 앱이 종료되므로 여기서 막고, 이어지는 코드는 다음 처리에서 실행한다
			try
			{
				var handle = GCHandle.FromIntPtr(context);
				var request = (AppleSignInRequest) handle.Target;
				request._context = IntPtr.Zero;
				handle.Free();
				var result = AppleSignInResults.From(status, identityToken, errorCode, message);
				MainThreadDispatcher.Post(() => request._completion.TrySetResult(result));
			}
			catch (Exception exception)
			{
				Logger.Error(exception, "Sign in with Apple callback failed");
			}
		}
	}
}
#endif
