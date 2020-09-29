namespace MarketWeatherAverages
{
    using System;
    using System.Diagnostics;

    using Microsoft.Extensions.Options;

    public class SecretRevealer : ISecretRevealer
    {
        private readonly Models.SecretStuff secrets;
        
        public SecretRevealer(IOptions<Models.SecretStuff> secrets)
        {
            this.secrets = secrets.Value ?? throw new System.ArgumentNullException(nameof(secrets));
        }

        public string GetApiKey()
        {
            if (Debugger.IsAttached)
            {
                return this.secrets.NOAAToken;
            }
            else
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOAAToken")))
                {
                    Environment.SetEnvironmentVariable("NOAAToken", "YqYKLmKNWIoTvBdYbwePgxxBAUnKVqqc");
                }
            }

            return Environment.GetEnvironmentVariable("NOAAToken");
        }
    }
}
