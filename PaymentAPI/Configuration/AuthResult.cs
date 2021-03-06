using System.Collections.Generic;

namespace PaymentAPI.Configuration
{
    public class AuthResult
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public string Messages {get; set;}
        public string UserId {get;set;}
    }
}