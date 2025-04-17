//namespace CryptoSimulator.DTOs
//{
//    public class  PasswordChangeResult
//    {
//        public bool Succeeded { get; private set; }
//        public IEnumerable<string> Errors { get; private set; } = Enumerable.Empty<string>();

//        public static PasswordChangeResult Success()
//        {
//            return new PasswordChangeResult
//            {
//                Succeeded = true,
//                Errors = Enumerable.Empty<string>()
//            };
//        }

//        public static PasswordChangeResult Failure(params string[] errors)
//        {
//            return new PasswordChangeResult
//            {
//                Succeeded = false,
//                Errors = errors ?? Enumerable.Empty<string>()
//            };
//        }
//    }
//}
