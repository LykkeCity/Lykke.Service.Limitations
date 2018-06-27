using AzureStorage;
using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public abstract class ClientsCashOperationsRepositoryBase<T> : IClientStateRepository<List<T>>
        where T : ICashOperation
    {
        protected readonly IBlobStorage _blobStorage;
        protected readonly ILog _log;
        protected readonly string _container;

        public ClientsCashOperationsRepositoryBase(IBlobStorage blobStorage, ILog log)
        {
            _blobStorage = blobStorage;
            _log = log;
            _container = GetType().Name.ToLower();
        }

        public async Task SaveClientStateAsync(string clientId, List<T> state)
        {
            try
            {
                string json = state.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json);
                await _blobStorage.SaveBlobAsync(_container, clientId, data);
            }
            catch (Exception exc)
            {
                _log.WriteWarning(nameof(CashOperationsStateRepository), nameof(SaveClientStateAsync), exc.GetBaseException().Message);
            }
        }

        public async Task<List<T>> LoadClientStateAsync(string clientId)
        {
            try
            {
                bool blobExists = await _blobStorage.HasBlobAsync(_container, clientId);
                if (!blobExists)
                    return new List<T>();
                string json = await _blobStorage.GetAsTextAsync(_container, clientId);
                var result = json.DeserializeJson<List<T>>();
                return result ?? new List<T>();
            }
            catch (Exception exc)
            {
                _log.WriteError(nameof(CashOperationsStateRepository), nameof(LoadClientStateAsync), exc);
                throw;
            }
        }
    }
}
