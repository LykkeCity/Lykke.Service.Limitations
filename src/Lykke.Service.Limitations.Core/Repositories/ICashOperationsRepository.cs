﻿using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface ICashOperationsRepository : IClientStateRepository<List<CashOperation>>
    {
    }
}
