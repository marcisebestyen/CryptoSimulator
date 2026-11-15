using CryptoSimulator.DTOs;

namespace CryptoSimulator.Results
{
    public enum DepositStatus
    {
        Success,
        Invalid,
        CardNotFound
    }

    public class DepositOutcome
    {
        public DepositStatus Status { get; private set; }
        public DepositGetDto? Deposit { get; private set; }
        public string? Error { get; private set; }

        private DepositOutcome() { }

        public static DepositOutcome Success(DepositGetDto deposit)
        {
            return new DepositOutcome { Status = DepositStatus.Success, Deposit = deposit };
        }

        public static DepositOutcome Invalid(string message)
        {
            return new DepositOutcome { Status = DepositStatus.Invalid, Error = message };
        }

        public static DepositOutcome CardNotFound()
        {
            return new DepositOutcome { Status = DepositStatus.CardNotFound, Error = "Saved card not found." };
        }
    }
}
