namespace MuchHttp;

public class LoadTestResult
{
    private readonly IReadOnlyCollection<RequestResult> _requestResults;
    private readonly List<double> _sortedRequestMilliseconds;

    public int SuccessfulRequests { get; }

    public LoadTestResult(IReadOnlyCollection<RequestResult> requestResults)
    {
        if (requestResults.Count == 0)
            throw new ArgumentException("Must provide at least one request result.", nameof(requestResults));

        _requestResults = requestResults;
        _sortedRequestMilliseconds = requestResults
            .Select(requestResult => requestResult.Timing.TotalMilliseconds)
            .Order()
            .ToList();

        SuccessfulRequests = _requestResults.Count(requestResult => requestResult.IsSuccessful);
    }

    private int CompletedRequests => _requestResults.Count;

    public int FailedRequests => CompletedRequests - SuccessfulRequests;

    public double AverageMilliseconds => _sortedRequestMilliseconds.Average();
    public double MedianMilliseconds => _sortedRequestMilliseconds[_sortedRequestMilliseconds.Count / 2];
    public double MinMilliseconds => _sortedRequestMilliseconds[0];
    public double MaxMilliseconds => _sortedRequestMilliseconds[^1];

    public IEnumerable<AggregatedError> AggregatedErrors => _requestResults
        .Where(requestResult => !requestResult.IsSuccessful)
        .GroupBy(requestResult => requestResult.ErrorMessage!)
        .Select(group => new AggregatedError(group.Key, group.Count()));
}
