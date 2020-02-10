namespace MwmBuilder
{
  internal abstract class ContentBuildLogger
  {
    public abstract void LogImportantMessage(string message, params object[] messageArgs);

    public abstract void LogMessage(string message, params object[] messageArgs);

    public abstract void LogWarning(string helpLink, string message, params object[] messageArgs);
  }
}
