using System;

namespace MwmBuilder
{
  internal class MyConsoleLogger : IMyBuildLogger
  {
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
      string message1 = string.Format("{0}: {3}{1}{2}", (object) filename, (object) Environment.NewLine, (object) message, (object) str).Replace(Environment.NewLine, Environment.NewLine + "    ").Trim(' ', '\r', '\n');
      switch (messageType)
      {
        case MessageType.Processed:
          this.LogMessage(message1);
          break;
        case MessageType.Warning:
          this.LogWarning(message1);
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

    public void Close()
    {
    }

    public void LogImportantMessage(string message, params object[] messageArgs)
    {
      this.LogMessage(message, messageArgs);
    }

    public void LogMessage(string message, params object[] messageArgs)
    {
      this.WriteConsole(new ConsoleColor?(), message, messageArgs);
    }

    public void LogWarning(string message, params object[] messageArgs)
    {
      this.WriteConsole(new ConsoleColor?(ConsoleColor.Yellow), message, messageArgs);
    }

    public void LogError(string message, params object[] messageArgs)
    {
      this.WriteConsole(new ConsoleColor?(ConsoleColor.Red), message, messageArgs);
    }

    private void WriteConsole(ConsoleColor? color, string msg, params object[] args)
    {
      lock (Console.Out)
      {
        if (color.HasValue)
          Console.ForegroundColor = color.Value;
        Console.WriteLine(msg, args);
        Console.ResetColor();
      }
    }
  }
}
