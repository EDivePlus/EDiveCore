using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using UnityEditor.Localization.Plugins.Google;

namespace EDIVE.Localization.Editor
{
    public static class SheetsServiceProviderUtility
    {
        public static async UniTask TryConnectAsync(this SheetsServiceProvider provider)
        {
            var service = provider.GetInternalService();
            if (service != null)
                return;

            switch (provider.Authentication)
            {
                case AuthenticationType.OAuth:
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    var userCredential = await provider.AuthorizeOAuthAsync(cts.Token);
                    var sheetsService = new SheetsService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = userCredential,
                        ApplicationName = provider.ApplicationName,
                    });
                    provider.SetInternalService(sheetsService);
                    break;
                }
                case AuthenticationType.APIKey:
                {
                    var sheetsService = new SheetsService(new BaseClientService.Initializer
                    {
                        ApiKey = provider.ApiKey,
                        ApplicationName = provider.ApplicationName
                    });
                    provider.SetInternalService(sheetsService);
                    break;
                }
                case AuthenticationType.None:
                default:
                    throw new Exception("No connection credentials. You must provide either OAuth2.0 credentials or an Api Key.");
            }
        }

        private static SheetsService GetInternalService(this SheetsServiceProvider provider)
        {
            return (SheetsService) GetInternalServiceField()?.GetValue(provider);
        }

        private static void SetInternalService(this SheetsServiceProvider provider, SheetsService service)
        {
            GetInternalServiceField()?.SetValue(provider, service);
        }

        private static FieldInfo GetInternalServiceField()
        {
            return typeof(SheetsServiceProvider).GetField("m_SheetsService", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
