namespace Utils.Logging
{
    public class CloudWatchLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly CloudWatchLogsProvider _provider;

        public CloudWatchLogger(string categoryName, CloudWatchLogsProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                var message = formatter(state, exception);
                _provider.EnqueueLog(logLevel, _categoryName, message, exception);
            }
        }
    }
}
