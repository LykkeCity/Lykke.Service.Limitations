using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Domain
{
    public interface IClientTierLogRecord
    {
        string DataOld { get; set; }
        string DataNew { get; set; }
        string Changer { get; set; }
        DateTime ChangeDate { get; set; }
    }
}
