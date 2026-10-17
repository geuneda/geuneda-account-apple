// Geuneda.Account.Apple 의 Sign in with Apple 플러그인(AppleSignInRequest.cs)
// 상태 값은 AppleSignInStatus 와 같다: 0 성공, 1 취소, 2 실패, 3 지원 안 함(iOS 13 미만)
// ARC 로 컴파일한다(meta 의 CompileFlags -fobjc-arc). 콜백은 메인 스레드에서 부른다(ASAuthorizationController 위임)
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

typedef void (*GeunedaAppleSignInCallback)(intptr_t context, int status, const char* identityToken, long errorCode, const char* message);

API_AVAILABLE(ios(13.0))
@interface GeunedaAppleSignInRequest : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@property (nonatomic, assign) intptr_t context;
@property (nonatomic, assign) GeunedaAppleSignInCallback callback;
@property (nonatomic, strong) ASAuthorizationController* controller;
@property (nonatomic, assign) BOOL finished;
@end

// 컨트롤러의 delegate 는 약한 참조라 시트가 끝날 때까지 요청을 붙잡아 둔다. 키는 C# 의 GCHandle 값이고 다음 요청이 같은 값을 다시 쓸 수 있다
static NSMutableDictionary<NSNumber*, GeunedaAppleSignInRequest*>* GeunedaAppleSignInPending()
{
    static NSMutableDictionary<NSNumber*, GeunedaAppleSignInRequest*>* pending;
    static dispatch_once_t once;
    dispatch_once(&once, ^{ pending = [[NSMutableDictionary alloc] init]; });
    return pending;
}

// 앱 창을 찾는다: 키 창, 없으면 활성 장면의 첫 창(앱 시작 직후에는 키 창이 아직 없을 수 있다)
static UIWindow* GeunedaPresentationWindow()
{
    UIWindow* fallback = nil;

    for (UIScene* scene in [UIApplication sharedApplication].connectedScenes)
    {
        if (![scene isKindOfClass:[UIWindowScene class]])
        {
            continue;
        }

        for (UIWindow* window in ((UIWindowScene*)scene).windows)
        {
            if (window.isKeyWindow)
            {
                return window;
            }

            if (fallback == nil && scene.activationState == UISceneActivationStateForegroundActive)
            {
                fallback = window;
            }
        }
    }

    return fallback;
}

@implementation GeunedaAppleSignInRequest

// 콜백은 요청마다 정확히 한 번이다(위임 결과와 취소 중 먼저 온 것). C# 은 이 콜백에서 GCHandle 을 푼다. 메인 스레드에서만 부른다
- (void)finishWithStatus:(int)status token:(NSString*)token errorCode:(long)errorCode message:(NSString*)message
{
    if (self.finished)
    {
        return;
    }

    self.finished = YES;
    self.callback(self.context, status, token.UTF8String, errorCode, message.UTF8String);
}

// 대기 목록에서 이 요청만 뺀다(같은 키를 다음 요청이 쓸 수 있다). 빼면 이 객체가 해제될 수 있으므로 마지막에 부른다
- (void)leave
{
    intptr_t context = self.context;

    if (GeunedaAppleSignInPending()[@(context)] == self)
    {
        [GeunedaAppleSignInPending() removeObjectForKey:@(context)];
    }
}

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithAuthorization:(ASAuthorization*)authorization
{
    NSString* token = nil;

    if ([authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]])
    {
        NSData* data = ((ASAuthorizationAppleIDCredential*)authorization.credential).identityToken;
        token = data != nil ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] : nil;
    }

    [self finishWithStatus:(token != nil ? 0 : 2) token:token errorCode:0 message:@"no identity token"];
    [self leave];
}

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithError:(NSError*)error
{
    int status = error.code == ASAuthorizationErrorCanceled ? 1 : 2;
    [self finishWithStatus:status token:nil errorCode:(long)error.code message:error.localizedDescription ?: @""];
    [self leave];
}

- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController*)controller
{
    return GeunedaPresentationWindow();
}

@end

// nonce 는 C# 이 서버 nonce 원문을 SHA-256 16진 소문자로 바꾼 값이다. 애플은 이 값을 id 토큰의 nonce 클레임에 그대로 넣는다
extern "C" void GeunedaAppleSignIn_Start(intptr_t context, const char* nonce, GeunedaAppleSignInCallback callback)
{
    if (@available(iOS 13.0, *))
    {
        ASAuthorizationAppleIDRequest* appleIdRequest = [[[ASAuthorizationAppleIDProvider alloc] init] createRequest];
        appleIdRequest.nonce = [NSString stringWithUTF8String:nonce];
        ASAuthorizationController* controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[ appleIdRequest ]];
        GeunedaAppleSignInRequest* request = [[GeunedaAppleSignInRequest alloc] init];
        request.context = context;
        request.callback = callback;
        request.controller = controller;
        controller.delegate = request;
        controller.presentationContextProvider = request;
        GeunedaAppleSignInPending()[@(context)] = request;
        [controller performRequests];
        return;
    }

    callback(context, 3, NULL, 0, "Sign in with Apple needs iOS 13 or later");
}

// 진행 중인 요청을 취소로 끝낸다(콜백으로 C# 핸들 해제). iOS 16 이상은 시트도 닫고 요청을 놓는다.
// 그 아래는 시트를 닫을 방법이 없어, 시트가 끝나 위임이 올 때까지 요청을 붙잡아 둔다(그 결과는 이미 끝난 요청이라 버린다)
extern "C" void GeunedaAppleSignIn_Cancel(intptr_t context)
{
    if (@available(iOS 13.0, *))
    {
        GeunedaAppleSignInRequest* request = GeunedaAppleSignInPending()[@(context)];
        [request finishWithStatus:1 token:nil errorCode:ASAuthorizationErrorCanceled message:@"Sign in with Apple was cancelled by the app"];

        if (@available(iOS 16.0, *))
        {
            ASAuthorizationController* controller = request.controller;
            [request leave];
            [controller cancel];
        }
    }
}
