using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Kyc.Abstractions.Domain.Profile;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Models
{
    public class NotificationProjection
    {
        private readonly IClientTierRepository _clientTierRepository;

        public NotificationProjection(
            IClientTierRepository clientTierRepository
            )
        {
            _clientTierRepository = clientTierRepository;
        }

        public async Task Handle(ChangeStatusEvent cmd)
        {
            if (cmd.NewStatus == "Ok")
            {
                string currentClientTierId = await _clientTierRepository.GetClientTierIdAsync(cmd.ClientId);
                if (currentClientTierId == null)
                {
                    string defaultTierId = await _clientTierRepository.GetDefaultTierIdAsync();
                    await _clientTierRepository.SetClientTierAsync(cmd.ClientId, defaultTierId);
                }
            }
        }
    }
}
