namespace MuchHttp;

public class ConcurrentCounter(int initialValue)
{
    private int _value = initialValue;

    public bool TryDecrement() => Interlocked.Decrement(ref _value) >= 0;
}
