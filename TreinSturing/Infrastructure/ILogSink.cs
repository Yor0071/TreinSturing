namespace TreinSturing.Infrastructure
{
    public interface ILogSink
    {
        void Info(string message);
        void Error(string message);
    }
}

