using System;

namespace WmsReferral.Business.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string TraceId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Message { get; set; }
        public bool ShowErrorMessage => !string.IsNullOrEmpty(Message);
        public int StatusCode { get; set; } = 200;
    }
}
