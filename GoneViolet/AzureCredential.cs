using Azure.Identity;

namespace GoneViolet
{
    public static class AzureCredential
    {
        public static DefaultAzureCredential DefaultAzureCredential { get; } = CreateDefaultAzureCredential();

        public static DefaultAzureCredentialOptions GetDefaultAzureCredentialOptions()
        {
            return new DefaultAzureCredentialOptions()
            {
                ExcludeAzureCliCredential = false,
                ExcludeAzurePowerShellCredential = false,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeEnvironmentCredential = false,
                ExcludeManagedIdentityCredential = false,
                ExcludeVisualStudioCodeCredential = false,
                ExcludeVisualStudioCredential = false
            };
        }

        private static DefaultAzureCredential CreateDefaultAzureCredential() => new DefaultAzureCredential(GetDefaultAzureCredentialOptions());
    }
}
