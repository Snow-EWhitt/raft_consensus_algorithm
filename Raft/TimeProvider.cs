namespace Raft;

public interface ITimeProvider
{
  DateTime UtcNow { get; }
}

public class RealTimeProvider : ITimeProvider
{
  public DateTime UtcNow => DateTime.UtcNow;
}