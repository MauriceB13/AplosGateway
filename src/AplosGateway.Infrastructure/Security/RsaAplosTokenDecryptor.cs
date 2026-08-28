using System.Security.Cryptography;
using System.Text;
using AplosGateway.Core.Security;

namespace AplosGateway.Infrastructure.Security;

public sealed class RsaAplosTokenDecryptor : IAplosTokenDecryptor
{
    public string Decrypt(string encryptedToken, string privateKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new ArgumentException(
                "Encrypted Aplos token cannot be empty.",
                nameof(encryptedToken));
        }

        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new ArgumentException(
                "Aplos private key cannot be empty.",
                nameof(privateKey));
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(
                RemoveWhitespace(encryptedToken));

            var privateKeyBytes = Convert.FromBase64String(
                RemoveWhitespace(privateKey));

            using var rsa = RSA.Create();

            rsa.ImportPkcs8PrivateKey(
                privateKeyBytes,
                out _);

            var decryptedBytes = rsa.Decrypt(
                encryptedBytes,
                RSAEncryptionPadding.Pkcs1);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException(
                "The Aplos token or private key is not valid Base64.",
                exception);
        }
        catch (CryptographicException exception)
        {
            throw new CryptographicException(
                "Unable to decrypt the Aplos access token using the configured private key.",
                exception);
        }
    }

    private static string RemoveWhitespace(string value)
    {
        return string.Concat(
            value.Where(character => !char.IsWhiteSpace(character)));
    }
}