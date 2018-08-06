// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Limitations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines values for CurrencyOperationType.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CurrencyOperationType
    {
        [EnumMember(Value = "CardCashIn")]
        CardCashIn,
        [EnumMember(Value = "CardCashOut")]
        CardCashOut,
        [EnumMember(Value = "CryptoCashIn")]
        CryptoCashIn,
        [EnumMember(Value = "CryptoCashOut")]
        CryptoCashOut,
        [EnumMember(Value = "SwiftTransfer")]
        SwiftTransfer,
        [EnumMember(Value = "SwiftTransferOut")]
        SwiftTransferOut,
        [EnumMember(Value = "TotalCashIn")]
        TotalCashIn,
        [EnumMember(Value = "TotalCashOut")]
        TotalCashOut
    }
    internal static class CurrencyOperationTypeEnumExtension
    {
        internal static string ToSerializedValue(this CurrencyOperationType? value)
        {
            return value == null ? null : ((CurrencyOperationType)value).ToSerializedValue();
        }

        internal static string ToSerializedValue(this CurrencyOperationType value)
        {
            switch( value )
            {
                case CurrencyOperationType.CardCashIn:
                    return "CardCashIn";
                case CurrencyOperationType.CardCashOut:
                    return "CardCashOut";
                case CurrencyOperationType.CryptoCashIn:
                    return "CryptoCashIn";
                case CurrencyOperationType.CryptoCashOut:
                    return "CryptoCashOut";
                case CurrencyOperationType.SwiftTransfer:
                    return "SwiftTransfer";
                case CurrencyOperationType.SwiftTransferOut:
                    return "SwiftTransferOut";
                case CurrencyOperationType.TotalCashIn:
                    return "TotalCashIn";
                case CurrencyOperationType.TotalCashOut:
                    return "TotalCashOut";
            }
            return null;
        }

        internal static CurrencyOperationType? ParseCurrencyOperationType(this string value)
        {
            switch( value )
            {
                case "CardCashIn":
                    return CurrencyOperationType.CardCashIn;
                case "CardCashOut":
                    return CurrencyOperationType.CardCashOut;
                case "CryptoCashIn":
                    return CurrencyOperationType.CryptoCashIn;
                case "CryptoCashOut":
                    return CurrencyOperationType.CryptoCashOut;
                case "SwiftTransfer":
                    return CurrencyOperationType.SwiftTransfer;
                case "SwiftTransferOut":
                    return CurrencyOperationType.SwiftTransferOut;
                case "TotalCashIn":
                    return CurrencyOperationType.TotalCashIn;
                case "TotalCashOut":
                    return CurrencyOperationType.TotalCashOut;
            }
            return null;
        }
    }
}
