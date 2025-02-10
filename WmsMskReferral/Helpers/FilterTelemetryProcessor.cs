using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace WmsMskReferral.Helpers
{
    public class FilterTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public FilterTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request &&
                request.Url.AbsolutePath.StartsWith("/health"))
            {
                //filter healthprobe and return without calling next processor
                return;
            }

            if (!string.IsNullOrEmpty(item.Context.Operation.SyntheticSource)) 
            { 
                //remove sythnetic sources i.e. bots and web tests
                return; 
            }

            _next.Process(item);
        }
    }
}
