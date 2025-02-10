using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace WmsElectiveCarePortal.Helpers
{
    public class FilterTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FilterTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public void Initialize(ITelemetry telemetry)
        {
            if ((_httpContextAccessor.HttpContext?.Request.Path.Value?
                .StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                .GetValueOrDefault())
            {
                // We don't want to track health checks.
                if (telemetry is ISupportAdvancedSampling advancedSampling)
                    advancedSampling.ProactiveSamplingDecision = SamplingDecision.SampledOut;

            }
        }
    }
}
