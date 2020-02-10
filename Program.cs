namespace MwmBuilder
{
  internal class Program
  {
    public static int Main(string[] args)
    {
      MyConsoleLogger myConsoleLogger = new MyConsoleLogger();
      return new ProgramContext().Work((object) args, new IMyBuildLogger[1]
      {
        (IMyBuildLogger) myConsoleLogger
      });
    }
  }
}
