namespace MwmBuilder
{
  public interface IMyBuildLogger
  {
    void LogMessage(MessageType messageType, string message, string filename = "");

    void Close();
  }
}
