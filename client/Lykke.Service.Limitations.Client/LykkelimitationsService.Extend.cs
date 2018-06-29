using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Lykke.Service.Limitations.Client.AutorestClient
{    
    /// <summary>
    /// Used to prevent memory leak in RetryPolicy
    /// </summary>
    public partial class LykkelimitationsService
    {
        public LykkelimitationsService(Uri baseUri, HttpClient client) : base(client)
        {
            Initialize();
            BaseUri = baseUri ?? throw new ArgumentNullException("baseUri");
        }
    }
}
