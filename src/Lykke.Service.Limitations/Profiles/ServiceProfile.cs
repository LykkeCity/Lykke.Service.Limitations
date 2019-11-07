using AutoMapper;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Client.Models.Response;
using Lykke.Service.Limitations.Core.Domain;

namespace Lykke.Service.Limitations.Profiles
{
    [UsedImplicitly]
    public class ServiceProfile : Profile
    {
        public ServiceProfile()
        {
            CreateMap<LimitationCheckResult, LimitationCheckResponse>(MemberList.Destination);
        }
    }
}
