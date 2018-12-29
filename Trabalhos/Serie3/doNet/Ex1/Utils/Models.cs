using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Utils
{
    public static class Status
    {
        public static int Success { get; set; } = 200;
        public static int Timeout { get; set; } = 204;
        public static int FormatInvalid { get; set; } = 400;
        public static int MissingQueue { get; set; } = 404;
        public static int ServerError { get; set; } = 500;
        public static int UnavailableService { get; set; } = 503;
    }

    // To represent a JSON request
    public class Request
    {
        public String Method { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }

        public override String ToString()
        {
            return $"Method: {Method}, Headers: {Headers}, Payload: {Payload}";
        }
    }

    // To represent a JSON response
    public class Response
    {
        public int Status { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }
    }
}
