namespace MuchHttp.Visualization;

public class ConsoleBlock
{
    private const int EllipsisChars = 3;

    private readonly int _maxPropertyWidth;

    private ConsoleBlock(int maxPropertyWidth)
    {
        if (maxPropertyWidth < 1)
            throw new ArgumentException("Block width must be positive.", nameof(maxPropertyWidth));
        _maxPropertyWidth = maxPropertyWidth;
    }

    public static void Create(int maxPropertyWidth, string heading, Action<ConsoleBlock> consoleBlockAction)
    {
        Console.WriteLine(heading);
        consoleBlockAction(new ConsoleBlock(maxPropertyWidth));
        Console.WriteLine();
    }

    public void WriteProperty(string property, object value)
    {
        if (property.Length > _maxPropertyWidth)
            property = property[..(_maxPropertyWidth - EllipsisChars)];

        Console.WriteLine($" {property.PadRight(_maxPropertyWidth, '.')}: {value}");
    }

    public static void FromHeading(string heading)
    {
        Console.WriteLine(heading);
        Console.WriteLine();
    }

    public static void FromException(Exception exception)
    {
        Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");
        Console.WriteLine();
    }

    public static void Colored(ConsoleColor color, Action consoleAction)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        consoleAction();
        Console.ForegroundColor = previousColor;
    }
}
