namespace MuchHttp.Visualization;

public sealed class ConsoleProgressBar : IProgress
{
    public required int Width { get; init; }

    public void Report(int current, int total)
    {
        var progressWidth = Width * current / total;
        var progressChars = string.Empty.PadRight(progressWidth, '\u2588').PadRight(Width, ' ');
        var progressText = $"{current} / {total}";
        Console.Write($"\r[{progressChars}] {progressText}");
    }

    public void Complete() => Console.Write("\n\n");
}
