using System.Diagnostics.Metrics;

namespace Gateway.Metrics;

public class GatewayMetrics
{
    private readonly Meter _meter;

    // Request Routing Metrics
    private readonly Counter<long> _requestsTotal;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _requestsFailed;

    // Downstream Service Metrics
    private readonly Histogram<double> _downstreamServiceLatency;
    private readonly Counter<long> _downstreamServiceErrors;

    public GatewayMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Gateway");

        // Request Metrics
        _requestsTotal = _meter.CreateCounter<long>(
            "gateway_requests_total",
            description: "Total number of requests routed through gateway");

        _requestDuration = _meter.CreateHistogram<double>(
            "gateway_request_duration_seconds",
            unit: "s",
            description: "Duration of requests processed by gateway");

        _requestsFailed = _meter.CreateCounter<long>(
            "gateway_requests_failed_total",
            description: "Total number of failed requests");

        // Downstream Metrics
        _downstreamServiceLatency = _meter.CreateHistogram<double>(
            "gateway_downstream_service_latency_seconds",
            unit: "s",
            description: "Latency of downstream service calls");

        _downstreamServiceErrors = _meter.CreateCounter<long>(
            "gateway_downstream_service_errors_total",
            description: "Total number of downstream service errors");
    }

    public void RecordRequest(string route, string method, int statusCode, double durationSeconds)
    {
        _requestsTotal.Add(1,
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status_code", statusCode));

        _requestDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("method", method));

        if (statusCode >= 400)
        {
            _requestsFailed.Add(1,
                new KeyValuePair<string, object?>("route", route),
                new KeyValuePair<string, object?>("status_code", statusCode));
        }
    }

    public void RecordDownstreamCall(string service, double latencySeconds, bool success)
    {
        _downstreamServiceLatency.Record(latencySeconds,
            new KeyValuePair<string, object?>("service", service));

        if (!success)
        {
            _downstreamServiceErrors.Add(1,
                new KeyValuePair<string, object?>("service", service));
        }
    }
}
