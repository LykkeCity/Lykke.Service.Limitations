using ProtoBuf;

namespace Lykke.Service.Limitations.Services.Contracts.FxPaygate
{
    [ProtoContract]
    public class TransferCreatedEvent
    {
        [ProtoMember(1)]
        public string OrderId { get; set; }
        [ProtoMember(2)]
        public double Amount { get; set; }
        [ProtoMember(3)]
        public string TransferId { get; set; }
        [ProtoMember(4)]
        public string AssetId { get; set; }
        [ProtoMember(5)]
        public string ClientId { get; set; }
    }
}
