using System.Collections.Generic;

namespace PaymentAPI.Configuration
{
    public class ResponseResult
    {
        public bool Success { get; set; }
        public string Method { get; set; }
        public List<string> Data { get; set; }
        public List<string> Errors { get; set; }
    }
}