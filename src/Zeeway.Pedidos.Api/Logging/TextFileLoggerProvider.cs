using System.Text;
using Microsoft.Extensions.Logging;

namespace Zeeway.Pedidos.Api.Logging;

internal sealed class TextFileLoggerProvider : ILoggerProvider
{
    private static readonly object FileLock = new();
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
    private readonly string _logDirectory;

    public TextFileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        new TextFileLogger(categoryName, Write);

    public void Dispose()
    {
    }

    private void Write(string category, LogLevel level, EventId eventId, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.Now;
        var filePath = Path.Combine(_logDirectory, $"zeeway-{timestamp:yyyy-MM-dd}.txt");
        var entry = $"{timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {category} " +
                    $"EventId={eventId.Id}: {message}{Environment.NewLine}";

        if (exception is not null)
            entry += exception + Environment.NewLine;

        lock (FileLock)
            File.AppendAllText(filePath, entry, Utf8WithoutBom);
    }

    private sealed class TextFileLogger : ILogger
    {
        private readonly string _category;
        private readonly Action<string, LogLevel, EventId, string, Exception?> _write;

        public TextFileLogger(
            string category,
            Action<string, LogLevel, EventId, string, Exception?> write)
        {
            _category = category;
            _write = write;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            ArgumentNullException.ThrowIfNull(formatter);
            _write(_category, logLevel, eventId, formatter(state, exception), exception);
        }
    }
}
