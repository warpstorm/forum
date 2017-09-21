using System;
using System.Security.Cryptography;
using System.Text;
using Forum3.Models.DataModels;
using Microsoft.AspNetCore.Identity;

namespace Forum3.Helpers {
	// Source: https://github.com/jsgoupil/membership2owin/blob/master/Identity/SQLMembershipPasswordHasher.cs
	public class CompatibilityHasher : PasswordHasher<ApplicationUser> {
		public override string HashPassword(ApplicationUser user, string password) => base.HashPassword(user, password);

		public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword) {
			var passwordProperties = hashedPassword.Split('|');
			var passwordHash = passwordProperties.Length == 3 ? passwordProperties[0] : hashedPassword;
			var passwordFormat = passwordProperties.Length == 3 ? int.Parse(passwordProperties[1]) : 0;

			// Made up number for new format
			if ((passwordProperties.Length != 3) || (passwordFormat == 4)) {
				return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
			}
			else {
				var salt = passwordProperties[2];
				var encryptedPassword = EncryptPassword(providedPassword, passwordFormat, salt);

				if (string.Equals(encryptedPassword, passwordHash, StringComparison.CurrentCultureIgnoreCase))
					return PasswordVerificationResult.SuccessRehashNeeded;

				return PasswordVerificationResult.Failed;
			}
		}

		string EncryptPassword(string password, int passwordFormat, string salt) {
			// MembershipPasswordFormat.Clear
			if (passwordFormat == 0)
				return password;

			var bIn = Encoding.Unicode.GetBytes(password);
			var bSalt = Convert.FromBase64String(salt);
			byte[] bRet = null;

			// MembershipPasswordFormat.Hashed 
			if (passwordFormat == 1) {
				var hm = HashAlgorithm.Create("SHA1");

				if (hm is KeyedHashAlgorithm kha) {
					if (kha.Key.Length == bSalt.Length) {
						kha.Key = bSalt;
					}
					else if (kha.Key.Length < bSalt.Length) {
						var bKey = new byte[kha.Key.Length];
						Buffer.BlockCopy(bSalt, 0, bKey, 0, bKey.Length);
						kha.Key = bKey;
					}
					else {
						var bKey = new byte[kha.Key.Length];

						for (var iter = 0; iter < bKey.Length;) {
							var len = Math.Min(bSalt.Length, bKey.Length - iter);
							Buffer.BlockCopy(bSalt, 0, bKey, iter, len);
							iter += len;
						}

						kha.Key = bKey;
					}

					bRet = kha.ComputeHash(bIn);
				}
				else {
					var bAll = new byte[bSalt.Length + bIn.Length];

					Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
					Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);

					bRet = hm.ComputeHash(bAll);
				}
			}

			return Convert.ToBase64String(bRet);
		}
	}
}