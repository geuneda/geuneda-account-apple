#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple.Editor
{
	/// <summary>
	/// iOS 빌드의 Xcode 프로젝트에 Sign in with Apple capability(entitlement <c>com.apple.developer.applesignin</c>)를 넣습니다.
	/// 이미 entitlements 파일이 있으면 그 파일에 더합니다. iOS 빌드 대상일 때만 컴파일합니다(UnityEditor.iOS.Xcode).
	/// </summary>
	internal sealed class AppleSignInCapability : IPostprocessBuildWithReport
	{
		public int callbackOrder => 100;

		public void OnPostprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.iOS)
			{
				return;
			}

			var outputPath = report.summary.outputPath;
			var projectPath = PBXProject.GetPBXProjectPath(outputPath);
			var project = new PBXProject();
			project.ReadFromFile(projectPath);

			var mainTarget = project.GetUnityMainTargetGuid();
			var entitlements = project.GetBuildPropertyForAnyConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS") ?? "Unity-iPhone/Unity-iPhone.entitlements";

			var capabilities = new ProjectCapabilityManager(projectPath, entitlements, null, mainTarget);
			capabilities.AddSignInWithApple();
			capabilities.WriteToFile();
		}
	}
}
#endif
