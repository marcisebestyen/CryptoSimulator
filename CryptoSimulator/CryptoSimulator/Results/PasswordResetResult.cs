namespace CryptoSimulator.Results
{
    public class PasswordResetResult
    {
        public bool Succeeded { get; private set; }
        public string? NewRecoveryCode { get; private set; }
        public string Error { get; private set; } = string.Empty;

        public static PasswordResetResult Success(string newRecoveryCode) =>
            new() { Succeeded = true, NewRecoveryCode = newRecoveryCode };

        public static PasswordResetResult Failure(string error) =>
            new() { Succeeded = false, Error = error };
    }
}
