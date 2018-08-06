using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class ClientTierEntity: TableEntity
    {
        public string TierId { get; set; }
    }
}
