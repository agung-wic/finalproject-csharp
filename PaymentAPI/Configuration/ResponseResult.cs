using System.Collections.Generic;

namespace PaymentAPI.Configuration
{
    public class ResponseResult
    {
        public bool Success { get; set; }
        public string Method { get; set; }
        public List<object> Data { get; set; }
        public string Errors { get; set; }
    }
}