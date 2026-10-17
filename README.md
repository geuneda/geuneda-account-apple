# Geuneda Account Apple

[com.geuneda.account](https://github.com/geuneda/geuneda-account) 의 Sign in with Apple 제공자입니다. iOS 의 AuthenticationServices 를 쓰는 자체 플러그인으로 애플 id 토큰을 받습니다(외부 SDK 없음). 이 패키지를 설치한 게임에만 네이티브 코드와 Sign in with Apple capability 가 들어갑니다.

| 기능 | 타입 | 네임스페이스 |
|------|------|-------------|
| 제공자 | `AppleAuthProvider` | `Geuneda.Account.Apple` |

## 설치

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.geuneda.core": "https://github.com/geuneda/geuneda-core.git",
    "com.geuneda.http": "https://github.com/geuneda/geuneda-http.git",
    "com.geuneda.account": "https://github.com/geuneda/geuneda-account.git",
    "com.geuneda.account.apple": "https://github.com/geuneda/geuneda-account-apple.git"
  }
}
```

## 사용법

```csharp
using Geuneda.Account;
using Geuneda.Account.Apple;

options.Providers.Add(new AppleAuthProvider());

// 로그인 버튼에서(앱이 전면에 있을 때). AuthService 가 먼저 인증 서버에서 nonce 를 받아 넘긴다
var result = await auth.SignInAsync(AccountPlatforms.Apple, token);
```

서버에는 애플 검증기를 켭니다(com.geuneda.account 의 `Server~`).

```csharp
builder.Services.AddGeunedaAccount(...).AddApple(options => options.Audiences.Add("com.company.game"));   // iOS 번들 ID
```

## 설정

1. Apple Developer 의 App ID(번들 ID)에서 **Sign in with Apple** capability 를 켭니다. 프로비저닝 프로파일도 다시 받습니다.
2. iOS 빌드 후 처리기가 Xcode 프로젝트의 entitlements 에 `com.apple.developer.applesignin` 을 넣습니다. 따로 할 일은 없습니다.
3. 서버 `AddApple` 의 대상(audience)에 같은 번들 ID 를 넣습니다.

## 동작

- iOS 13 이상에서 애플 로그인 시트를 띄웁니다. 이름·이메일은 요청하지 않습니다(서버는 애플 사용자 고유 ID `sub` 로 계정을 찾음).
- id 토큰은 인증 서버가 발급한 nonce 에 묶습니다(replay 방어). `AuthService` 가 넘긴 nonce 원문의 SHA-256 16진 소문자를 애플 요청(`ASAuthorizationAppleIDRequest.nonce`)에 넣고, 애플은 그 값을 id 토큰의 `nonce` 클레임에 그대로 넣습니다. 원문은 `AuthService` 가 로그인 요청에 담아 서버로 보내고, 서버(`AppleIdTokenVerifier`)는 원문을 해시해 비교한 뒤 그 nonce 를 소비합니다. 그래서 같은 id 토큰을 다시 보내면 `InvalidNonce` 입니다.
- `IAuthProvider` 로 직접 부를 때는 `SignInAsync(nonce, token)` 에 서버 nonce 를 넘겨야 합니다(비면 `ArgumentException`). 보통은 `AuthService` 를 통해 씁니다.
- 사용자가 닫으면 `ResultCodes.Cancelled`, 그 밖의 실패(설정 안 됨, Apple 계정 없음 등)는 `AccountResultCodes.PlatformSignInFailed` 입니다. 메시지에 `ASAuthorizationError` 코드가 들어 있습니다.
- `SignInAsync` 의 취소 토큰을 취소하면 결과는 바로 `Cancelled` 이고, iOS 16 이상은 시트도 닫습니다.
- iOS 가 아니면(에디터, Android 등) `PlatformSignInFailed` 입니다. Android 에서 애플 로그인은 지원하지 않습니다.
- 애플은 앱에서 로그아웃하는 API 가 없어 `SignOutAsync` 는 아무것도 하지 않습니다. 연결 해제는 사용자가 iOS 설정에서 합니다.

## 요구 사항

- Unity 2021.3 이상, iOS 13 이상(그 아래는 실패 결과)
- `com.geuneda.account` 0.3.0(nonce 를 받는 인증 서버도 0.3.0), `com.geuneda.core` 0.2.0, `com.cysharp.unitask` 2.5.10

변경 이력은 [CHANGELOG.md](CHANGELOG.md) 를 참고하세요.

## 라이선스

MIT. [LICENSE.md](LICENSE.md) 를 참고하세요.
