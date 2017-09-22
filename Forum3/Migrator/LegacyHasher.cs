using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Forum3.Helpers;
using Forum3.Models.DataModels;

namespace Forum3.Migrator {
	
	// All of this turned out to be unnecessary! ASP.NET Auth 2.0 already supports auto-updating old hashes.
	
	// Some sources I was looking at for this:
	// https://github.com/jsgoupil/membership2owin/blob/master/Identity/SQLMembershipPasswordHasher.cs
	// https://stackoverflow.com/questions/45873006/how-to-consume-and-asp-net-membership-database-in-asp-net-identity
	public class LegacyHasher : PasswordHasher<ApplicationUser> {
		public override string HashPassword(ApplicationUser user, string password) => base.HashPassword(user, password);

		public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string password) {
			password.ThrowIfNull(nameof(password));

			var result = base.VerifyHashedPassword(user, hashedPassword, password);

			if (result == PasswordVerificationResult.Failed)
				result = VerifyLegacyPassword(user, hashedPassword, password);

			return result;
		}

		PasswordVerificationResult VerifyLegacyPassword(ApplicationUser user, string hashedPassword, string password) {
			byte[] buffer4;

			if (hashedPassword == null)
				return PasswordVerificationResult.Failed;

			var src = Convert.FromBase64String(hashedPassword);

			if ((src.Length != 0x31) || (src[0] != 0))
				return PasswordVerificationResult.Failed;

			var dst = new byte[0x10];
			Buffer.BlockCopy(src, 1, dst, 0, 0x10);

			var buffer3 = new byte[0x20];
			Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);

			using (var bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8)) {
				buffer4 = bytes.GetBytes(0x20);
			}

			if (ByteArraysEqual(buffer3, buffer4))
				return PasswordVerificationResult.SuccessRehashNeeded;

			return PasswordVerificationResult.Failed;
		}

		public bool ByteArraysEqual(byte[] b1, byte[] b2) {
			if (b1 == b2)
				return true;

			if (b1 == null || b2 == null)
				return false;

			if (b1.Length != b2.Length)
				return false;

			for (var i = 0; i < b1.Length; i++) {
				if (b1[i] != b2[i])
					return false;
			}

			return true;
		}

		//public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword) {
		//	hashedPassword.ThrowIfNull(nameof(hashedPassword));
		//	providedPassword.ThrowIfNull(nameof(providedPassword));

		//	//if (user.PasswordFormat == 0) {
		//	//	providedPasswordHash = providedPassword;
		//	//}
		//	//else if (user.PasswordFormat == 1) {
		//	//	var providedPasswordSalt = user.PasswordSalt;
		//	//	HashPassword(providedPassword, out providedPasswordHash, ref providedPasswordSalt);
		//	//}
		//	//else
		//	//	throw new NotSupportedException("Encrypted passwords are not supported.");

		//	var providedPasswordHash = HashPassword2(providedPassword);

		//	if (providedPasswordHash == hashedPassword)
		//		return PasswordVerificationResult.Success;

		//	return PasswordVerificationResult.Failed;
		//}

		string HashPassword2(string password) {
			byte[] salt;
			byte[] buffer2;

			if (password == null) {
				throw new ArgumentNullException("password");
			}
			using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8)) {
				salt = bytes.Salt;
				buffer2 = bytes.GetBytes(0x20);
			}

			byte[] dst = new byte[0x31];
			Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
			Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
			return Convert.ToBase64String(dst);
		}

		string HashPassword(string password) {
			var passwordBytes = Encoding.Unicode.GetBytes(password);

			byte[] saltBytes = null;

			saltBytes = new byte[128 / 8];

			using (var rng = RandomNumberGenerator.Create()) {
				rng.GetBytes(saltBytes);
			}

			var totalBytes = new byte[saltBytes.Length + passwordBytes.Length];

			Buffer.BlockCopy(saltBytes, 0, totalBytes, 0, saltBytes.Length);
			Buffer.BlockCopy(passwordBytes, 0, totalBytes, saltBytes.Length, passwordBytes.Length);

			using (var hashAlgorithm = SHA1.Create()) {
				return Convert.ToBase64String(hashAlgorithm.ComputeHash(totalBytes));
			}

			//passwordSalt = Convert.ToBase64String(saltBytes);
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