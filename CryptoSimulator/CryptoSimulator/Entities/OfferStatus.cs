using System.Text.Json.Serialization;

namespace CryptoSimulator.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OfferStatus
    {
        Open,
        PartlySold,
        SoldOut,
        Cancelled
    }
}
