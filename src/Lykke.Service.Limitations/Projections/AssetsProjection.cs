using System.Threading.Tasks;
using Lykke.Common.Cache;
using Lykke.Service.Assets.Contract.Events;
using Lykke.Service.Limitations.Core.Domain;

namespace Lykke.Service.Limitations.Projections
{
    public class AssetsProjection
    {
        private readonly OnDemandDataCache<Asset> _assetsCache;

        public AssetsProjection(OnDemandDataCache<Asset> assetsCache)
        {
            _assetsCache = assetsCache;
        }

        private async Task Handle(AssetCreatedEvent evt)
        {
            _assetsCache.Set(evt.Id, new Asset
            {
                Id = evt.Id,
                Accuracy = evt.Accuracy,
                LowVolumeAmount = evt.LowVolumeAmount,
                CashoutMinimalAmount = evt.CashoutMinimalAmount
            });
        }

        public async Task Handle(AssetUpdatedEvent evt)
        {
            _assetsCache.Set(evt.Id, new Asset
            {
                Id = evt.Id,
                Accuracy = evt.Accuracy,
                LowVolumeAmount = evt.LowVolumeAmount,
                CashoutMinimalAmount = evt.CashoutMinimalAmount
            });
        }
    }
}
