using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ZoomAttendance.Helpers.Logging
{
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minimumLevel;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(string logDirectory, LogLevel minimumLevel = LogLevel.Information)
        {
            _logDirectory = logDirectory;
            _minimumLevel = minimumLevel;
            Directory.CreateDirectory(_logDirectory);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory, _minimumLevel));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        private sealed class FileLogger : ILogger
        {
            private static readonly object WriteLock = new();

            private readonly string _categoryName;
            private readonly string _logDirectory;
            private readonly LogLevel _minimumLevel;

            public FileLogger(string categoryName, string logDirectory, LogLevel minimumLevel)
            {
                _categoryName = categoryName;
                _logDirectory = logDirectory;
                _minimumLevel = minimumLevel;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var message = formatter(state, exception);
                if (string.IsNullOrWhiteSpace(message) && exception is null)
                    return;

                var logPath = Path.Combine(_logDirectory, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
                var entry =
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [{logLevel}] {_categoryName}{Environment.NewLine}" +
                    $"{message}{Environment.NewLine}" +
                    $"{(exception is null ? string.Empty : exception + Environment.NewLine)}";

                lock (WriteLock)
                {
                    File.AppendAllText(logPath, entry + Environment.NewLine);
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
