using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ukg_wfm_sync.Services;

public class KeyVaultService(string keyVaultUrl)
{
    private readonly SecretClient _client = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development"
        ? new SecretClient(new Uri(keyVaultUrl), new AzureCliCredential())
        : new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

    public async Task<string> GetSecret(string name)
    {
        try
        {
            Response<KeyVaultSecret> secret = await _client.GetSecretAsync(name);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KeyVault GetSecret Failed | SecretName: {name} | Error: {ex.Message}");
            throw;
        }
    }
}
