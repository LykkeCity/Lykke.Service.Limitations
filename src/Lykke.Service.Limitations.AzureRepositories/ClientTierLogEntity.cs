using Lykke.Service.Limitations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class ClientTierLogEntity: TableEntity, IClientTierLogRecord
    {
        public string OldTierId { get; set; }
        public string NewTierId { get; set; }
        public string Changer { get; set; }
        public DateTime ChangeDate { get; set; }
    }
}
