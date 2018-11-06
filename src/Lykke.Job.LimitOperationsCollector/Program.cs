using System.Threading.Tasks;
using Lykke.Sdk;

namespace Lykke.Job.LimitOperationsCollector
{
    internal sealed class Program
    {
        public static async Task Main()
        {
#if DEBUG
            await LykkeStarter.Start<Startup>(true);
#else
            await LykkeStarter.Start<Startup>(false);
#endif
        }
    }
}
