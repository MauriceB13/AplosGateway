using System.Security.Cryptography;
using System.Text;
using AplosGateway.Infrastructure.Security;

namespace AplosGateway.Tests.Security;

public sealed class RsaAplosTokenDecryptorTests
{
    [Fact]
    public void Decrypt_ReturnsOriginalPlainText()
    {
        const string plainText = "test-aplos-access-token";

        using var rsa = RSA.Create(2048);

        var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

        var encryptedBytes = rsa.Encrypt(
            Encoding.UTF8.GetBytes(plainText),
            RSAEncryptionPadding.Pkcs1);

        var encryptedTokenBase64 =
            Convert.ToBase64String(encryptedBytes);

        var decryptor = new RsaAplosTokenDecryptor();

        var result = decryptor.Decrypt(
            encryptedTokenBase64,
            privateKeyBase64);

        Assert.Equal(plainText, result);
    }

    [Fact]
    public void Decrypt_ThrowsWhenEncryptedTokenIsEmpty()
    {
        var decryptor = new RsaAplosTokenDecryptor();

        Assert.Throws<ArgumentException>(() =>
            decryptor.Decrypt("", "private-key"));
    }

    [Fact]
    public void Decrypt_ThrowsWhenPrivateKeyIsEmpty()
    {
        var decryptor = new RsaAplosTokenDecryptor();

        Assert.Throws<ArgumentException>(() =>
            decryptor.Decrypt("encrypted-token", ""));
    }
}