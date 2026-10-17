using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace

namespace Geuneda.Account.Apple
{
	/// <summary>
	/// 애플 로그인 요청에 넣을 nonce 입니다. 서버가 발급한 원문의 SHA-256(UTF-8) 16진 소문자를 넣고 원문은 인증 서버에만 보냅니다.
	/// 애플은 요청에 넣은 값을 id 토큰의 <c>nonce</c> 클레임에 그대로 넣고, 서버(<c>AppleIdTokenVerifier</c>)는 원문을 같은 방식으로 해시해 비교합니다.
	/// </summary>
	internal static class AppleNonce
	{
		public static string Hash(string nonce)
		{
			using (var sha = SHA256.Create())
			{
				var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(nonce));
				var hex = new StringBuilder(hash.Length * 2);

				foreach (var b in hash)
				{
					hex.Append(b.ToString("x2"));
				}

				return hex.ToString();
			}
		}
	}
}
