namespace OrderProcessing.Shared.Logging;

public static class Log
{
    private static readonly object _sync = new();

    private static void Write(string source, string message, ConsoleColor color)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Environment.CurrentManagedThreadId}]{source}-> {message}";
        lock (_sync)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }
    public static void Info(string source, string message) => Write(source, message, ConsoleColor.White);
    public static void Success(string source, string message) => Write(source, message, ConsoleColor.Green);
    public static void Error(string source, string message) => Write(source, message, ConsoleColor.Red);
}