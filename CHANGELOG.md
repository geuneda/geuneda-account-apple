# Changelog
이 패키지의 모든 주요 변경사항은 이 파일에 기록됩니다.

이 형식은 [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)를 기반으로 하며,
이 프로젝트는 [Semantic Versioning](http://semver.org/spec/v2.0.0.html)을 따릅니다.

## [0.1.0] - 2026-10-14

**New**:
- `AppleAuthProvider`: iOS 13 이상 Sign in with Apple(AuthenticationServices 자체 플러그인 `GeunedaAppleSignIn.mm`, 외부 SDK 없음). 애플 id 토큰을 자격으로 돌려줌. 사용자 닫기는 `Cancelled`, 그 밖의 실패는 `PlatformSignInFailed`(ASAuthorizationError 코드 포함), 취소 토큰으로 중단(iOS 16 이상은 시트도 닫음)
- iOS 빌드 후 처리기 `AppleSignInCapability`: Xcode entitlements 에 Sign in with Apple capability 추가
