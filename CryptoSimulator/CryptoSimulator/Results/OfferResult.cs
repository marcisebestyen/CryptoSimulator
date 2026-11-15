using CryptoSimulator.DTOs;

namespace CryptoSimulator.Results
{
    public enum OfferResultStatus
    {
        Success,
        Invalid,
        NotFound,
        NotOwner,
        Conflict
    }

    public class OfferResult
    {
        public OfferResultStatus Status { get; private set; }
        public MyOfferDto? Offer { get; private set; }
        public string? Error { get; private set; }

        private OfferResult() { }

        public static OfferResult Success(MyOfferDto? offer = null)
        {
            return new OfferResult { Status = OfferResultStatus.Success, Offer = offer };
        }

        public static OfferResult Invalid(string message)
        {
            return new OfferResult { Status = OfferResultStatus.Invalid, Error = message };
        }

        public static OfferResult NotFound(string message)
        {
            return new OfferResult { Status = OfferResultStatus.NotFound, Error = message };
        }

        public static OfferResult NotOwner()
        {
            return new OfferResult { Status = OfferResultStatus.NotOwner, Error = "Only the seller can cancel this offer." };
        }

        public static OfferResult Conflict(string message)
        {
            return new OfferResult { Status = OfferResultStatus.Conflict, Error = message };
        }
    }
}
