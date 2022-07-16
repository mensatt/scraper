using Microsoft.Extensions.Logging;

namespace MensattScraper.Telemetry;

public class LogDelegator : ILogger
{
    private readonly List<ILogger> _logConsumer;

    public LogDelegator(params ILogger?[] consumers)
    {
        // Nullability warning can be suppressed here, as we filter them out
        _logConsumer = consumers.Where(consumer => consumer != null).ToList()!;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _logConsumer.ForEach(logger => logger.Log(logLevel, eventId, state, exception, formatter));


    public bool IsEnabled(LogLevel logLevel) => _logConsumer.TrueForAll(logger => logger.IsEnabled(logLevel));


    public IDisposable BeginScope<TState>(TState state) => throw new NotSupportedException("Scopes are unsupported");
}