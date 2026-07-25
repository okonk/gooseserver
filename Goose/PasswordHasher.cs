using System;
using System.Security.Cryptography;
using System.Text;

namespace Goose
{
    /**
     * PasswordHasher, derives and verifies player password hashes
     *
     * Uses PBKDF2-HMAC-SHA256. This replaces the previous single-round MD5 scheme,
     * which offered no work factor and left a stolen database trivially crackable.
     *
     * The stored hash is self describing:
     *
     *     pbkdf2-sha256$<iterations>$<base64 derived key>
     *
     * The iteration count travels with the hash, so the work factor can be raised
     * later without locking existing players out - Verify honours whatever count the
     * stored hash was written with, and NeedsRehash reports when a hash predates the
     * current cost so it can be upgraded on next successful login.
     *
     * The salt is stored separately in the player's password_salt column as base64.
     *
     * Passwords are encoded as UTF8. The old code round tripped both the password and
     * the salt through Encoding.ASCII, which silently folded every byte above 0x7F to
     * '?' and threw away a large part of the salt entropy.
     *
     */
    public static class PasswordHasher
    {
        private const string Algorithm = "pbkdf2-sha256";

        private const int SaltBytes = 16;
        private const int KeyBytes = 32;

        /**
         * Current cost. Raise this over time as hardware improves; existing hashes keep
         * working because they carry their own iteration count.
         */
        public const int Iterations = 210000;

        /**
         * Create, derives a new hash and salt for a password
         *
         * Returns the encoded hash and the base64 salt, to be stored in the player's
         * password_hash and password_salt columns respectively.
         *
         */
        public static (string Hash, string Salt) Create(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);

            string encoded = Derive(password, salt, Iterations);

            return (encoded, Convert.ToBase64String(salt));
        }

        /**
         * Verify, checks a password against a stored hash and salt
         *
         * Returns false rather than throwing on malformed or missing stored values, so a
         * corrupt row denies login instead of taking down the caller.
         *
         */
        public static bool Verify(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt)) return false;

            byte[] salt;
            int iterations;
            byte[] expected;

            try
            {
                salt = Convert.FromBase64String(storedSalt);

                string[] parts = storedHash.Split('$');
                if (parts.Length != 3) return false;
                if (!parts[0].Equals(Algorithm, StringComparison.Ordinal)) return false;
                if (!int.TryParse(parts[1], out iterations)) return false;
                if (iterations <= 0) return false;

                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        /**
         * NeedsRehash, true if a stored hash was written with a weaker cost than current
         *
         * Callers can use this after a successful Verify to transparently upgrade the
         * stored hash to the current iteration count.
         *
         */
        public static bool NeedsRehash(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return true;

            string[] parts = storedHash.Split('$');
            if (parts.Length != 3) return true;
            if (!parts[0].Equals(Algorithm, StringComparison.Ordinal)) return true;
            if (!int.TryParse(parts[1], out int iterations)) return true;

            return iterations < Iterations;
        }

        private static string Derive(string password, byte[] salt, int iterations)
        {
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                KeyBytes);

            return Algorithm + "$" + iterations + "$" + Convert.ToBase64String(key);
        }
    }
}
