using System.IO;

namespace MwmBuilder
{
  internal class MyFileLogger : IMyBuildLogger
  {
    public StreamWriter InfoLog;
    public StreamWriter WarningLog;
    public StreamWriter ErrorLog;

    public void LogMessage(MessageType messageType, string message, string filename = "")
    {
      string str = string.Empty;
      switch (messageType)
      {
        case MessageType.Processed:
          str = "";
          break;
        case MessageType.Warning:
          str = "WARNING: ";
          break;
        case MessageType.Info:
          str = "";
          break;
        case MessageType.Error:
          str = "ERROR: ";
          break;
        case MessageType.UpToDate:
          str = "UpToDate ";
          break;
      }
      string message1 = string.Format("{0}: {2}{1}", (object) filename, (object) message, (object) str);
      switch (messageType)
      {
        case MessageType.Processed:
          this.LogMessage(message1);
          break;
        case MessageType.Warning:
          this.LogWarningInternal(message1);
          break;
        case MessageType.Info:
        case MessageType.UpToDate:
          this.LogMessage(message1);
          break;
        case MessageType.Error:
          this.LogError(message1);
          break;
      }
    }

    public void LogImportantMessage(string message, params object[] messageArgs)
    {
      this.LogMessage(message, messageArgs);
    }

    public void LogWarningInternal(string message, params object[] messageArgs)
    {
      this.LogWarning((string) null, message, messageArgs);
    }

    public void LogMessage(string message, params object[] messageArgs)
    {
      this.WriteLog(this.InfoLog, message, messageArgs);
    }

    public void LogWarning(string helpLink, string message, params object[] messageArgs)
    {
      this.WriteLog(this.WarningLog ?? this.InfoLog, message, messageArgs);
    }

    public void LogError(string message, params object[] messageArgs)
    {
      this.WriteLog(this.ErrorLog ?? this.WarningLog ?? this.InfoLog, message, messageArgs);
    }

    private void WriteLog(StreamWriter log, string msg, params object[] args)
    {
      if (log == null)
        return;
      lock (log)
        log.WriteLine(msg, args);
    }

    public void Close()
    {
      if (this.InfoLog != null)
        this.InfoLog.Close();
      if (this.WarningLog != null)
        this.WarningLog.Close();
      if (this.ErrorLog == null)
        return;
      this.ErrorLog.Close();
    }
  }
}
