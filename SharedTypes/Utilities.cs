using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Snakes;

public static class Utilities
{
    public const string AzureKeyvaultUrl = "https://sneks-kv.vault.azure.net/";
    private const string AzureStorageConnectionStringSecretName = "AzureStorageConnectionString";

    public static async Task<string> GetStorageConnectionString()
    {
        var secretClient = new SecretClient(new Uri(AzureKeyvaultUrl), new DefaultAzureCredential());
        var storageConnectionString = await secretClient.GetSecretAsync(AzureStorageConnectionStringSecretName);
        return storageConnectionString.Value.Value;
    }
}

