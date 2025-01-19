using System.Security.Cryptography;
using System.Text;

namespace VaccinaCare.Application.Ultils;

public class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit salt
    private const int HashSize = 32; // 256-bit hash

    /// <summary>
    /// Hashes a password with a randomly generated salt.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>A string containing the salt and hashed password, separated by a colon.</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.");

        using (var rng = new RNGCryptoServiceProvider())
        {
            // Generate a random salt
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            // Hash the password
            var hash = HashPasswordWithSalt(password, salt);

            // Combine the salt and hash into a single string
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="storedHash">The stored hash (salt and hash, separated by a colon).</param>
    /// <returns>True if the password matches, otherwise false.</returns>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        // Hash the input password with the stored salt
        var hashToVerify = HashPasswordWithSalt(password, salt);

        // Compare the stored hash with the computed hash
        return CryptographicOperations.FixedTimeEquals(hash, hashToVerify);
    }

    /// <summary>
    /// Hashes a password with a provided salt.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <param name="salt">The salt.</param>
    /// <returns>The hashed password as a byte array.</returns>
    private byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        using (var hmac = new HMACSHA256(salt))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}