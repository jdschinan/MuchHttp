namespace MuchHttp;

public record RequestResult(TimeSpan Timing, string? ErrorMessage = null)
{
    public bool IsSuccessful => ErrorMessage is null;
}
