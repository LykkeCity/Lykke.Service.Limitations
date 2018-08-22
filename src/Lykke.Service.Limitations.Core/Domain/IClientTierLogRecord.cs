using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Domain
{
    public interface IClientTierLogRecord
    {
        string OldTierId { get; set; }
        string NewTierId { get; set; }
        string Changer { get; set; }
        DateTime ChangeDate { get; set; }
    }
}
