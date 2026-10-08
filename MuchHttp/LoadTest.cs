using System.Collections.Concurrent;
using System.Diagnostics;

namespace MuchHttp;

public class LoadTest(HttpClient httpClient, Uri url, int concurrentRequests, int totalRequests)
{
    private const int UpdateProgressIntervalMilliseconds = 100;

    private readonly int _concurrentRequests = Math.Min(concurrentRequests, totalRequests);

    public async Task<LoadTestResult> PerformAsync(IProgress progress)
    {
        var remainingRequests = new ConcurrentCounter(totalRequests);
        var requestResults = new ConcurrentBag<RequestResult>();

        await CheckConnectionAsync();

        var updateProgressTask = UpdateProgressAsync();
        var workerTasks = Enumerable.Repeat(ProcessRequestsAsync, _concurrentRequests)
            .Select(taskFactory => taskFactory.Invoke())
            .ToArray();

        await Task.WhenAll(workerTasks);
        await updateProgressTask;

        return new LoadTestResult(requestResults);

        async Task ProcessRequestsAsync()
        {
            while (remainingRequests.TryDecrement())
            {
                var requestResult = await ProcessRequestAsync();
                requestResults.Add(requestResult);
            }
        }

        async Task UpdateProgressAsync()
        {
            await Task.Delay(UpdateProgressIntervalMilliseconds);
            while (requestResults.Count < totalRequests)
            {
                progress.Report(requestResults.Count, totalRequests);
                await Task.Delay(UpdateProgressIntervalMilliseconds);
            }

            progress.Report(totalRequests, totalRequests);
            progress.Complete();
        }
    }

    private async Task<RequestResult> ProcessRequestAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await httpClient.SendAsync(request);
            stopwatch.Stop();

            return response.IsSuccessStatusCode
                ? new RequestResult(stopwatch.Elapsed)
                : new RequestResult(stopwatch.Elapsed, $"HTTP status {(int)response.StatusCode}");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new RequestResult(stopwatch.Elapsed, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private async Task CheckConnectionAsync()
    {
        await httpClient.GetAsync(url);
    }
}
