namespace MuchHttp;

public interface IProgress
{
    void Report(int current, int total);
    void Complete();
}
