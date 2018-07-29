using AzureStorage;
using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public abstract class ClientsCashOperationsRepositoryBase<T> : IClientStateRepository<List<T>>
        where T : ICashOperation
    {
        protected readonly IBlobStorage _blobStorage;
        protected readonly ILog _log;
        protected readonly string _container;

        public ClientsCashOperationsRepositoryBase(IBlobStorage blobStorage, ILogFactory logFactory)
        {
            _blobStorage = blobStorage;
            _log = logFactory.CreateLog(this);
            _container = GetType().Name.ToLower();
        }

        public async Task SaveClientStateAsync(string filename, List<T> state)
        {
            try
            {
                string json = state.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json);
                await _blobStorage.SaveBlobAsync(_container, filename, data);
            }
            catch (Exception exc)
            {
                _log.Warning(exc.GetBaseException().Message, exc);
            }
        }

        public async Task<List<T>> LoadClientStateAsync(string filename)
        {
            try
            {
                bool blobExists = await _blobStorage.HasBlobAsync(_container, filename);
                if (!blobExists)
                    return null;
                string json = await _blobStorage.GetAsTextAsync(_container, filename);
                var result = json.DeserializeJson<List<T>>();
                return result ?? new List<T>();
            }
            catch (Exception exc)
            {
                _log.Error(exc);
                throw;
            }
        }
    }
}
