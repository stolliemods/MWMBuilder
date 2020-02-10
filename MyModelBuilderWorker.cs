using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MwmBuilder
{
  internal class MyModelBuilderWorker
  {
    public readonly string Name;
    private Thread m_workerThread;

    public MyModelBuilderWorker(string name)
    {
      this.Name = name;
    }

    public void Start(
      string outputDir,
      string defaultParameters,
      bool force,
      bool skipAppVersionCheck,
      bool exportXML,
      bool checkOpenBoundaries,
      string lodDistances)
    {
      List<string> stringList = new List<string>();
      stringList.Add(Process.GetCurrentProcess().MainModule.FileName);
      stringList.Add(string.Format("/i:{0}", (object) this.Name));
      if (force)
        stringList.Add("/f");
      if (skipAppVersionCheck)
        stringList.Add("/g");
      if (exportXML)
        stringList.Add("/e");
      if (checkOpenBoundaries)
        stringList.Add("/checkOpenBoundaries");
      if (outputDir != null)
        stringList.Add(string.Format("/o:{0}", (object) outputDir));
      if (lodDistances != null)
        stringList.Add(string.Format("/d:{0}", (object) lodDistances));
      this.m_workerThread = new Thread(new ParameterizedThreadStart(new ProgramContext().WorkThread));
      this.m_workerThread.Start((object) stringList.ToArray());
    }

    public void WaitForExit()
    {
      this.m_workerThread.Join();
    }
  }
}
