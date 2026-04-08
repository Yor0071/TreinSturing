namespace TreinSturing.Infrastructure
{
    public interface IPlcReader
    {
        bool IsConnected { get; }
        int Connect(string ip, int rack, int slot);
        void Disconnect();
        byte[] ReadDbBytes(int dbNumber, int start, int length);
    }
}
