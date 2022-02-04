using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snakes;
public static class Utilities
{
    private const string AzureStorageEnvVarName2 = "AzureStorageConnectionString";
    private const string AzureStorageEnvVarName1 = "azure-storage-connection-string";

    public static string GetStorageConnectionString()
    {
        var res = Environment.GetEnvironmentVariable(AzureStorageEnvVarName1);
        if (string.IsNullOrWhiteSpace(res))
        {
            res = Environment.GetEnvironmentVariable(AzureStorageEnvVarName2);
            if (string.IsNullOrWhiteSpace(res))
            {
                throw new InvalidOperationException($"No Azure Storage Connection string specified in {AzureStorageEnvVarName1} or {AzureStorageEnvVarName2}");
            }
        }

        return res!;
    }
}
