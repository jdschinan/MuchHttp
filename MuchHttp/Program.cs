using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using MuchHttp;
using MuchHttp.Visualization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var urlOption = new Option<Uri>("--url", "-u")
{
    Description = "The URL to direct all HTTP requests to",
    HelpName = "URL",
    CustomParser = ParseUri
};

var concurrentRequestsOption = new Option<int>("--concurrent", "-c")
{
    Description = "The maximum number of concurrently sent requests",
    HelpName = "concurrent requests"
};

var totalRequestsOption = new Option<int>("--total", "-n")
{
    Description = "The total number of requests to send",
    HelpName = "total requests"
};

var rootCommand = new RootCommand("Perform HTTP GET requests against a specified URL with a configurable level of concurrency.")
{
    urlOption,
    concurrentRequestsOption,
    totalRequestsOption
};

rootCommand.SetAction(parseResult => PerformAsync(
    parseResult.GetRequiredValue(urlOption),
    parseResult.GetRequiredValue(concurrentRequestsOption),
    parseResult.GetRequiredValue(totalRequestsOption)
));

return await rootCommand.Parse(args).InvokeAsync();

async Task PerformAsync(Uri url, int concurrentRequests, int totalRequests)
{
    ConsoleBlock.Colored(ConsoleColor.Cyan, () =>
        ConsoleBlock.FromHeading($"Starting load test with {totalRequests} requests ({concurrentRequests} concurrent)")
    );

    try
    {
        var progressBar = new ConsoleProgressBar { Width = 64 };

        using var httpClient = new HttpClient();
        var loadTestResult = await new LoadTest(httpClient, url, concurrentRequests, totalRequests).PerformAsync(progressBar);

        ConsoleBlock.Create(24, "Summary:", block =>
        {
            block.WriteProperty("Successful requests", loadTestResult.SuccessfulRequests);
            block.WriteProperty("Failed requests", loadTestResult.FailedRequests);
            block.WriteProperty("Average", $"{loadTestResult.AverageMilliseconds:N2} ms");
            block.WriteProperty("Median", $"{loadTestResult.MedianMilliseconds:N2} ms");
            block.WriteProperty("Min", $"{loadTestResult.MinMilliseconds:N2} ms");
            block.WriteProperty("Max", $"{loadTestResult.MaxMilliseconds:N2} ms");
        });

        if (loadTestResult.FailedRequests > 0)
        {
            ConsoleBlock.Colored(ConsoleColor.Red, () =>
            {
                ConsoleBlock.Create(64, "Errors:", block =>
                {
                    foreach (var error in loadTestResult.AggregatedErrors)
                        block.WriteProperty(error.Message, error.Count);
                });
            });
        }
    }
    catch (Exception exception)
    {
        ConsoleBlock.Colored(ConsoleColor.Red, () =>
            ConsoleBlock.FromException(exception)
        );
    }
}

Uri ParseUri(ArgumentResult result)
{
    if (Uri.TryCreate(result.Tokens.Single().Value, UriKind.Absolute, out var uri))
        return uri;

    result.AddError("Invalid URL format.");
    return null!;
}
