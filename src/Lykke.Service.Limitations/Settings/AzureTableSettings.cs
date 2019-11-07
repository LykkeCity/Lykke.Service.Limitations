using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly]
    public class AzureTableSettings
    {
        [AzureTableCheck]
        public string ConnectionString { get; set; }
    }
}
