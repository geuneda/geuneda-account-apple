# Geuneda.Account.Apple - AI 에이전트 가이드

> **관련 파일**: `CLAUDE.md`는 이 파일을 감쌉니다 — `CLAUDE.md`가 아니라 `AGENTS.md`를 편집하세요. `README.md`는 사용자 대상 문서입니다.

## 1. 패키지 개요
- **패키지**: `com.geuneda.account.apple`, 어셈블리 `Geuneda.Account.Apple`, 에디터 `Geuneda.Account.Apple.Editor`
- **의존성**: `com.geuneda.account`(IAuthProvider, PlatformCredential, AccountPlatforms, AccountResultCodes), `com.geuneda.core`(Result, MainThreadDispatcher, ModuleLogger), UniTask
- **이유(geuneda 결정 D3)**: 네이티브 코드·capability 를 가진 연동은 기능 패키지(account)와 따로 둡니다. Unity 는 네이티브 플러그인(.mm)의 define 제약을 무시하므로 한 패키지 안 define 으로 켜고 끌 수 없습니다(2026-10-14 실험).

## 2. 구조

| 파일 | 역할 |
|------|------|
| `Runtime/AppleAuthProvider.cs` | 공개 제공자. iOS 플레이어가 아니면 실패 결과 |
| `Runtime/AppleSignInRequest.cs` | `#if UNITY_IOS && !UNITY_EDITOR`. 요청 하나. GCHandle 을 네이티브 context 로 넘기고 네이티브 콜백에서만 풂 |
| `Runtime/AppleSignInResults.cs`, `AppleSignInStatus.cs` | 네이티브 상태 → 결과 코드(에디터 테스트 대상) |
| `Runtime/Plugins/iOS/GeunedaAppleSignIn.mm` | ASAuthorizationController. meta: iOS 만, `CompileFlags: -fobjc-arc`, `FrameworkDependencies: AuthenticationServices;` |
| `Editor/AppleSignInCapability.cs` | `#if UNITY_IOS`, `IPostprocessBuildWithReport`: `ProjectCapabilityManager.AddSignInWithApple()`(기존 `CODE_SIGN_ENTITLEMENTS` 가 있으면 그 파일) |

## 3. 규칙

- **콜백은 정확히 한 번**: 네이티브 요청 객체(`GeunedaAppleSignInRequest`)가 `finished` 플래그로 위임 결과와 앱 취소 중 먼저 온 것만 콜백합니다. 대기 사전에서 뺄 때(`leave`)는 그 키의 값이 이 요청일 때만 뺍니다(키는 GCHandle 값이라 다음 요청이 같은 값을 쓸 수 있음). 빼면 객체가 해제될 수 있으므로 `leave` 는 마지막에 부릅니다.
- **네이티브 콜백 안에서 예외 금지**: reverse P/Invoke 에서 예외가 나면 앱이 종료됩니다. `OnCompleted` 는 try/catch 로 막고, 결과는 `MainThreadDispatcher.Post` 로 넘겨 이어지는 사용자 코드가 콜백 안에서 돌지 않게 합니다.
- **C# static 상태 없음(D2)**: 콜백 함수 포인터(`static readonly`)만 둡니다. 요청 연결은 GCHandle 로 합니다. 핸들은 네이티브 콜백에서만 풀어, 취소로 먼저 끝나도 늦은 콜백이 해제된 핸들을 쓰지 않습니다. 네이티브에는 진행 중인 요청을 붙잡아 두는 사전 하나가 있습니다(컨트롤러 delegate 가 약한 참조라 필요, 요청이 끝나면 비워짐).
- **늦은 취소 무시**: 네이티브 콜백이 오면 C# 이 `_context` 를 0 으로 바꾸고, `Cancel` 은 0 이면 아무것도 하지 않습니다. 완료 결과와 취소가 함께 메인 스레드 큐에 들어간 뒤 완료 처리에서 다시 로그인하면, 이전 요청의 취소가 같은 GCHandle 값을 받은 새 요청을 취소하는 것을 막습니다.
- **취소**: 취소 콜백은 아무 스레드에서나 오므로 `MainThreadDispatcher.Post` 로 메인 스레드에서 네이티브 취소를 부릅니다. 네이티브는 요청을 취소로 끝내 콜백(핸들 해제)하고, iOS 16 이상은 `[controller cancel]` 로 시트를 닫고 요청을 놓습니다. 그 아래는 시트를 닫을 방법이 없어 위임이 올 때까지 요청을 붙잡아 둡니다. 결과는 그 자리에서 `Cancelled` 입니다.
- 상태 값(0 성공, 1 취소, 2 실패, 3 미지원)은 C# enum 과 .mm 이 같아야 합니다.

## 4. 테스트

- `Tests/EditMode/AppleAuthProviderTest.cs`: 상태 → 코드 변환, 에디터에서 실패 결과·로그아웃 없음, 이미 취소된 토큰.
- 기기: 플러그인 컴파일·entitlements·호출 경로는 iOS 시뮬레이터에서 확인했습니다(2026-10-14: Apple 계정 없는 시뮬레이터에서 시스템 "Apple 계정에 로그인" 알림까지 도달, 8초 뒤 취소 → `Cancelled`). 실제 로그인은 Sign in with Apple 을 켠 App ID 와 기기(또는 Apple 계정에 로그인한 시뮬레이터)가 필요합니다.
