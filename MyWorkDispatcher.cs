using System.Collections.Generic;
using System.Linq;

namespace MwmBuilder
{
  internal class MyWorkDispatcher
  {
    private List<MyModelBuilderWorker> m_workers = new List<MyModelBuilderWorker>();
    private const int BufferSize = 131072;
    private string m_outputDir;
    private string m_defaultParameters;
    private List<string> m_files;
    private int m_itemCount;
    private int m_processedFiles;
    private bool m_logUpToDate;
    private bool m_force;
    private bool m_skipAppCheck;
    private bool m_exportXML;
    private bool m_checkOpenBoundaries;
    private string m_lodDistances;

    public float Progress
    {
      get
      {
        lock (this.m_files)
          return (float) this.m_processedFiles / (float) this.m_itemCount;
      }
    }

    public MyWorkDispatcher(
      IEnumerable<string> files,
      string outputDir,
      string defaultParameters,
      bool force,
      bool skipAppCheck,
      bool exportXML,
      bool checkOpenBoundaries,
      string lodDistances)
    {
      this.m_outputDir = outputDir;
      this.m_defaultParameters = defaultParameters;
      this.m_files = files.Reverse<string>().ToList<string>();
      this.m_itemCount = this.m_files.Count;
      this.m_skipAppCheck = skipAppCheck;
      this.m_force = force;
      this.m_exportXML = exportXML;
      this.m_checkOpenBoundaries = checkOpenBoundaries;
      this.m_lodDistances = lodDistances;
    }

    public bool Run(int workerCount, bool logUpToDate)
    {
      bool flag = true;
      this.m_logUpToDate = logUpToDate;
      for (int index = 0; index < workerCount; ++index)
        this.StartWorker(string.Format("Worker_{0}", (object) index), this.m_files);
      int itemCount = this.m_itemCount;
      for (int index = 0; index < itemCount; ++index)
      {
        foreach (MyModelBuilderWorker worker in this.m_workers)
          this.AssignJob();
      }
      foreach (MyModelBuilderWorker worker in this.m_workers)
        ProgramContext.Names.Enqueue((string) null);
      foreach (MyModelBuilderWorker worker in this.m_workers)
        worker.Start(this.m_outputDir, this.m_defaultParameters, this.m_force, this.m_skipAppCheck, this.m_exportXML, this.m_checkOpenBoundaries, this.m_lodDistances);
      foreach (MyModelBuilderWorker worker in this.m_workers)
        worker.WaitForExit();
      return flag;
    }

    private void AssignJob()
    {
      string instance = (string) null;
      lock (this.m_files)
      {
        int index = this.m_files.Count - 1;
        if (index >= 0)
        {
          instance = this.m_files[index];
          this.m_files.RemoveAt(index);
        }
      }
      ProgramContext.Names.Enqueue(instance);
    }

    private void StartWorker(string workerName, List<string> files)
    {
      this.m_workers.Add(new MyModelBuilderWorker(workerName));
    }
  }
}
