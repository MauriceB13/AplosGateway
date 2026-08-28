namespace AplosGateway.Core.Security;

public interface IAplosTokenDecryptor
{
    string Decrypt(string encryptedToken, string privateKey);
}