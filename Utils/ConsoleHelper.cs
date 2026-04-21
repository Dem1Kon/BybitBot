namespace BybitBot.Utils;

/// <summary>
/// Вспомогательные методы для консольного вывода
/// </summary>
public static class ConsoleHelper
{
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Вывод информационного сообщения (синий)
    /// </summary>
    public static void WriteInfo(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод сообщения об успехе (зеленый)
    /// </summary>
    public static void WriteSuccess(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод сообщения об ошибке (красный)
    /// </summary>
    public static void WriteError(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод предупреждения (желтый)
    /// </summary>
    public static void WriteWarning(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод торгового сообщения (магента)
    /// </summary>
    public static void WriteTrade(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод заголовка
    /// </summary>
    public static void WriteHeader(string title)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('=', 60));
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Вывод разделителя
    /// </summary>
    public static void WriteSeparator()
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 60));
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Очистка текущей строки
    /// </summary>
    public static void ClearLine()
    {
        lock (_lock)
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}