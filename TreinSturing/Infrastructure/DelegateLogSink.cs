using System;

namespace TreinSturing.Infrastructure
{
    public sealed class DelegateLogSink : ILogSink
    {
        private readonly Action<string> _info;
        private readonly Action<string> _error;

        public DelegateLogSink(Action<string> info, Action<string> error)
        {
            _info = info;
            _error = error;
        }

        public void Info(string message)
        {
            _info?.Invoke(message);
        }

        public void Error(string message)
        {
            _error?.Invoke(message);
        }
    }
}