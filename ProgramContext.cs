using MwmBuilder.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using VRage.Collections;

namespace MwmBuilder
{
    public class ProgramContext
    {
        private static Dictionary<string, object> m_vars = new Dictionary<string, object>()
    {
      {
        "RescaleFactor",
        (object) "0.01"
      }
    };
        private static MyBuildLoggers m_buildLogger = new MyBuildLoggers();
        private static MyFileLogger m_fileLogger = new MyFileLogger();
        public static MyConcurrentQueue<string> Names = new MyConcurrentQueue<string>();
        private static string m_materialsPath = "C:\\KeenSWH\\Sandbox\\MediaBuild\\MEContent\\Materials";
        private MyProgramArgs m_args = new MyProgramArgs();
        private int m_processed;
        private int m_count;
        private DateTime? appLastChangeDateTime;
        private Stopwatch m_watch;
        private static List<MyMaterialsLib> m_materialLibs;
        public static string OutputDir;

        public static Dictionary<string, object> DefaultModelParameters
        {
            get
            {
                return ProgramContext.m_vars;
            }
        }

        public int Work(object data, IMyBuildLogger[] loggers = null)
        {
            if (loggers != null)
            {
                foreach (IMyBuildLogger logger in loggers)
                    ProgramContext.m_buildLogger.AddLogger(logger);
            }
            ProgramContext.m_buildLogger.AddLogger((IMyBuildLogger)ProgramContext.m_fileLogger);
            this.m_watch = new Stopwatch();
            this.m_watch.Start();
            this.m_args.RegisterArg("s", "/s:SOURCE", (string)null, "Path to source FBX file(s), directory or file. Directories are read recursively. Defaults to current directory.");
            this.m_args.RegisterArg("o", "/o:OUTPUT", "C:\\Program Files (x86)\\Steam\\SteamApps\\common\\SpaceEngineers\\Content\\", "Path to output");
            this.m_args.RegisterArg("m", "/m:MASK", "*.fbx", "File mask of files to process, defaults to *.FBX");
            this.m_args.RegisterArg("l", "/l:LOGFILE", (string)null, "Path to logfile");
            this.m_args.RegisterArg("t", "/t:THREADS", "1", "Run model build on several threads");
            this.m_args.RegisterArg("cob", (string)null, (string)null, "Check if model contains open boundaries");
            this.m_args.RegisterArg("e", (string)null, (string)null, "Force XML export");
            this.m_args.RegisterArg("a", (string)null, (string)null, "Split logfile to separate errors and warnings to separate logfiles .warn log and .err log");
            this.m_args.RegisterArg("u", (string)null, (string)null, "Log file when file is up to date");
            this.m_args.RegisterArg("f", (string)null, (string)null, "Rebuild files even when up-to-date");
            this.m_args.RegisterArg("g", (string)null, (string)null, "Don't compare app build date to files");
            this.m_args.RegisterArg("p", (string)null, (string)null, "Wait for key after build");
            this.m_args.RegisterArg("i", (string)null, (string)null, (string)null);
            this.m_args.RegisterArg("c", (string)null, (string)null, (string)null);
            this.m_args.RegisterArg("do", "/do", (string)null, "Override LOD Distances");
            this.m_args.RegisterArg("d", "/d:LODS", (string)null, "Float values separated by space, defining default values for LOD 1-n");
            this.m_args.RegisterArg("x", "/x:MATERIALSLIB", "C:\\KeenSWH\\Sandbox\\MediaBuild\\MEContent\\Materials", "Path to materials library");

            string[] args = (string[])data;
            try
            {
                this.m_args.Parse(args);
                if (this.m_args.Empty)
                {
                    this.m_args.WriteHelp();
                    return 1;
                }
                if (((IEnumerable<string>)args).FirstOrDefault<string>((Func<string, bool>)(a => a.StartsWith("/o"))) == null)
                {
                    ProgramContext.m_buildLogger.LogMessage(MessageType.Error, "Output path was not specified!", "");
                    return 1;
                }
                if (!Directory.Exists(this.m_args.GetValue("o")))
                {
                    ProgramContext.m_buildLogger.LogMessage(MessageType.Error, "Cannot find output path: " + this.m_args.GetValue("o"), "");
                    return 1;
                }
                if (this.m_args.GetValue("f") == null && this.m_args.GetValue("g") == null)
                    this.appLastChangeDateTime = new DateTime?(File.GetLastWriteTimeUtc(Assembly.GetCallingAssembly().Location));
                if (this.m_args.GetValue("i") != null)
                {
                    this.RunAsJobWorker();
                    return 0;
                }

                if (this.m_args.GetValue("l") != null)
                {
                    string path = this.m_args.GetValue("l");
                    m_fileLogger.InfoLog = new StreamWriter(path);
                    if (this.m_args.GetValue("a") != null)
                    {
                        m_fileLogger.WarningLog = new StreamWriter(Path.ChangeExtension(path, ".warn" + Path.GetExtension(path)));
                        m_fileLogger.ErrorLog = new StreamWriter(Path.ChangeExtension(path, ".err" + Path.GetExtension(path)));
                    }
                }

                string[] strArray = this.LoadFiles(this.m_args.GetValue("s"), this.m_args.GetValue("m"));

                this.setMaterialsPathForSource(this.m_args.GetValue("x"));
                ProgramContext.LoadMaterialLibs();
                this.m_count = strArray.Length;

                if (this.m_args.GetValue("t") != null)
                {
                    int int32 = Convert.ToInt32(this.m_args.GetValue("t"));
                    if (int32 > 1)
                    {
                        int num = new MyWorkDispatcher((IEnumerable<string>)strArray, this.m_args.GetValue("o"), (string)null, this.m_args.GetValue("f") != null, this.m_args.GetValue("g") != null, this.m_args.GetValue("e") != null, this.m_args.GetValue("cob") != null, this.m_args.GetValue("d")).Run(int32, this.m_args.GetValue("u") != null) ? 0 : 1;
                        ProgramContext.m_buildLogger.Close();
                        this.WaitForKey();
                        return num;
                    }
                }
                foreach (string file in strArray)
                    this.ProcessFileSafe(file);
            }
            catch (Exception ex)
            {
                ProgramContext.m_buildLogger.LogMessage(MessageType.Error, ex.ToString(), "");
                this.WaitForKey();
                return 1;
            }
            ProgramContext.m_buildLogger.Close();
            this.WaitForKey();
            return 0;
        }

        private void setMaterialsPathForSource(string sourcePath)
        {
            if (sourcePath == null)
                return;

            if (!Directory.Exists(Path.GetFullPath(sourcePath)))
                ProgramContext.m_buildLogger.LogMessage(MessageType.Warning, "Could not find Materials library folder.", sourcePath);
            else
                ProgramContext.m_materialsPath = Path.GetFullPath(sourcePath);
        }

        private void ProcessFileSafe(string file)
        {
            try
            {
                bool overrideLods = false;
                if (this.m_args.GetValue("do") != null)
                    overrideLods = true;

                if (this.ProcessFile(file,
                    ProgramContext.OutputDir = this.m_args.GetValue("o"),
                    ProgramContext.m_vars, this.m_args.GetValue("e") != null,
                    this.m_args.GetValue("f") != null,
                    this.m_args.GetValue("cob") != null,
                    overrideLods ? this.m_args.GetValue("do") : this.m_args.GetValue("d"),
                    overrideLods))
                {
                    lock (ProgramContext.m_buildLogger)
                        ProgramContext.m_buildLogger.LogMessage(MessageType.Processed,
                            string.Format("{1}: Finished in {0}s",
                            (object)this.m_watch.Elapsed.TotalSeconds.ToString("f1"),
                            this.m_args.GetValue("i") == null ? (object)"" : (object)this.m_args.GetValue("i")),
                            file);
                }
                ++this.m_processed;
            }
            catch (Exception ex)
            {
                ProgramContext.m_buildLogger.LogMessage(MessageType.Error, file + ":" + Environment.NewLine + ex.ToString(), "");
            }
            this.UpdateProgress((float)this.m_processed / (float)this.m_count);
        }

        private void WaitForKey()
        {
            try
            {
                if (this.m_args.GetValue("p") == null || Console.IsInputRedirected)
                    return;
                Console.WriteLine("Build done, waiting for key...");
                Console.ReadKey();
            }
            catch
            {
            }
        }

        public void WorkThread(object data)
        {
            this.Work(data, (IMyBuildLogger[])null);
        }

        public static MyModelConfiguration ImportXml(string xmlFile)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(MyModelConfiguration));
            new XmlSerializerNamespaces().Add(string.Empty, string.Empty);
            using (FileStream fileStream = File.Open(xmlFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return (MyModelConfiguration)xmlSerializer.Deserialize((Stream)fileStream);
        }

        private void RunAsJobWorker()
        {
            this.m_args.GetValue("i");
            while (ProgramContext.Names.Count > 0)
            {
                string instance;
                ProgramContext.Names.TryDequeue(out instance);
                if (!string.IsNullOrEmpty(instance))
                    this.ProcessFileSafe(instance);
            }
        }

        public static void ExportXml(string xmlFile, MyModelConfiguration configuration)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(MyModelConfiguration));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream fileStream = File.Open(xmlFile, FileMode.OpenOrCreate))
                {
                    xmlSerializer.Serialize((Stream)memoryStream, (object)configuration, namespaces);
                    memoryStream.Position = 0L;
                    fileStream.Position = 0L;
                    if (memoryStream.Length == fileStream.Length && ProgramContext.StreamEquals((Stream)memoryStream, (Stream)fileStream))
                        return;
                    fileStream.SetLength(0L);
                    fileStream.Position = 0L;
                    memoryStream.Position = 0L;
                    memoryStream.WriteTo((Stream)fileStream);
                }
            }
        }

        private static bool StreamEquals(Stream streamA, Stream streamB)
        {
            byte[] buffer1 = new byte[(int)ushort.MaxValue];
            byte[] buffer2 = new byte[(int)ushort.MaxValue];
            int num1;
            do
            {
                num1 = streamA.Read(buffer1, 0, buffer1.Length);
                int num2 = streamB.Read(buffer2, 0, buffer2.Length);
                if (num1 != num2)
                    return false;
                for (int index = 0; index < num1; ++index)
                {
                    if ((int)buffer1[index] != (int)buffer2[index])
                        return false;
                }
            }
            while (num1 > 0);
            return true;
        }

        private void UpdateProgress(float progress)
        {
            float num = 100f * progress;
            if ((double)num > 100.0)
                num = 100f;
            if (Console.IsInputRedirected)
                return;
            try
            {
                Console.Title = string.Format("Mwm Builder: {0}%", (object)num.ToString("f1"));
            }
            catch
            {
            }
        }

        private string[] LoadFiles(string src, string mask)
        {
            if (src == null)
                src = Directory.GetCurrentDirectory();
            List<string> list = new List<string>();
            string str = src;
            char[] chArray = new char[1] { ';' };
            foreach (string path in str.Split(chArray))
            {
                if ((File.GetAttributes(path) & FileAttributes.Directory) == (FileAttributes)0)
                {
                    if (File.Exists(path))
                        list.Add(path);
                    else
                        ProgramContext.m_buildLogger.LogMessage(MessageType.Error, string.Format("Target file '{0}' does not exists", (object)path), "");
                }
                else
                {
                    string[] files = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
                    list.AddArray<string>(files);
                }
            }
            return ((IEnumerable<string>)list.ToArray()).Select<string, FileInfo>((Func<string, FileInfo>)(s => new FileInfo(s))).OrderByDescending<FileInfo, long>((Func<FileInfo, long>)(s => s.Length)).Select<FileInfo, string>((Func<FileInfo, string>)(s => s.FullName)).ToArray<string>();
        }

        private bool ProcessFile(
          string file,
          string outputDir,
          Dictionary<string, object> defaultVars,
          bool exportXml,
          bool forceBuild,
          bool checkOpenBoundaries,
          string lodDistances,
          bool overrideLods)
        {
            DateTime sourceDateTime = File.GetLastWriteTimeUtc(file);
            string str1 = Path.ChangeExtension(file, "xml");
            bool flag = false;
            MyModelConfiguration configuration = (MyModelConfiguration)null;
            if (File.Exists(str1))
            {
                DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(str1);
                if (lastWriteTimeUtc > sourceDateTime)
                    sourceDateTime = lastWriteTimeUtc;
                configuration = ProgramContext.ImportXml(str1);
            }
            else
                flag = exportXml;
            if (configuration == null)
                configuration = new MyModelConfiguration()
                {
                    Name = "Default",
                    Parameters = defaultVars.Select<KeyValuePair<string, object>, MyModelParameter>((Func<KeyValuePair<string, object>, MyModelParameter>)(s => new MyModelParameter()
                    {
                        Name = s.Key,
                        Value = s.Value.ToString()
                    })).ToArray<MyModelParameter>()
                };
            if (configuration.MaterialRefs != null)
            {
                foreach (MyModelParameter materialRef in configuration.MaterialRefs)
                    ProgramContext.LoadMaterialByRef(materialRef.Name);
            }
            byte[] havokCollisionShapes = this.ReadExternalFile("hkt", file, ref sourceDateTime);
            if (this.appLastChangeDateTime.HasValue && this.appLastChangeDateTime.Value > sourceDateTime)
                sourceDateTime = this.appLastChangeDateTime.Value;
            FileInfo fileInfo = new FileInfo(MyModelProcessor.GetOutputPath(file, outputDir));
            if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > sourceDateTime && !forceBuild)
            {
                if (flag)
                    ProgramContext.ExportXml(str1, configuration);
                return false;
            }
            ProgramContext.ItemInfo itemInfo = new ProgramContext.ItemInfo()
            {
                Index = 0,
                Path = file,
                Name = Path.GetFileNameWithoutExtension(file),
                Configuration = configuration
            };
            float[] numArray;
            if (lodDistances == null)
                numArray = new float[0];
            else
                numArray = Array.ConvertAll<string, float>(lodDistances.Trim().Split(' '), new Converter<string, float>(float.Parse));
            float[] lodDistances1 = numArray;
            this.ProcessItem(itemInfo, outputDir, havokCollisionShapes, checkOpenBoundaries, lodDistances1, overrideLods);
            if (exportXml)
            {
                List<string> materialsToRef = new List<string>();
                foreach (MyMaterialConfiguration material in configuration.Materials)
                {
                    if (ProgramContext.GetMaterialByRef(material.Name) != null)
                        materialsToRef.Add(material.Name);
                }
                if (materialsToRef.Count > 0)
                {
                    configuration.Materials = ((IEnumerable<MyMaterialConfiguration>)configuration.Materials).Where<MyMaterialConfiguration>((Func<MyMaterialConfiguration, bool>)(x => !materialsToRef.Contains(x.Name))).ToArray<MyMaterialConfiguration>();
                    if (configuration.MaterialRefs == null)
                    {
                        configuration.MaterialRefs = materialsToRef.ConvertAll<MyModelParameter>((Converter<string, MyModelParameter>)(x => new MyModelParameter()
                        {
                            Name = x
                        })).ToArray();
                    }
                    else
                    {
                        List<MyModelParameter> myModelParameterList = new List<MyModelParameter>();
                        foreach (string str2 in materialsToRef)
                        {
                            string mat = str2;
                            if (!((IEnumerable<MyModelParameter>)configuration.MaterialRefs).Any<MyModelParameter>((Func<MyModelParameter, bool>)(x => x.Name == mat)))
                                myModelParameterList.Add(new MyModelParameter()
                                {
                                    Name = mat
                                });
                        }
                        configuration.MaterialRefs = ((IEnumerable<MyModelParameter>)configuration.MaterialRefs).Union<MyModelParameter>((IEnumerable<MyModelParameter>)myModelParameterList).ToArray<MyModelParameter>();
                    }
                }
                ProgramContext.ExportXml(str1, configuration);
            }
            return true;
        }

        private void ProcessItem(
          ProgramContext.ItemInfo item,
          string outputDir,
          byte[] havokCollisionShapes,
          bool checkOpenBoundaries,
          float[] lodDistances,
          bool overrideLods)
        {
            if (item.Configuration == null)
                ProgramContext.m_buildLogger.LogMessage(MessageType.Info, string.Format("Model skipped! No configuration for '{0}'", (object)item.Path), "");
            else
                new MyModelBuilder().Build(item.Path,
                    "tmp",
                    outputDir,
                    item.Configuration,
                    havokCollisionShapes,
                    checkOpenBoundaries,
                    lodDistances,
                    overrideLods,
                    new Func<string, MyMaterialConfiguration>(ProgramContext.GetMaterialByRef),
                    (IMyBuildLogger)ProgramContext.m_buildLogger);
        }

        private byte[] ReadExternalFile(string extension, string file, ref DateTime sourceDateTime)
        {
            byte[] buffer = (byte[])null;
            string path = Path.ChangeExtension(file, extension);
            if (File.Exists(path))
            {
                DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
                if (lastWriteTimeUtc > sourceDateTime)
                    sourceDateTime = lastWriteTimeUtc;
                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                long length = fileStream.Length;
                if (length > 0L)
                {
                    buffer = new byte[length];
                    fileStream.Read(buffer, 0, Convert.ToInt32(length));
                }
                fileStream.Close();
            }
            return buffer;
        }

        private void ParseVariables(string vars)
        {
            ProgramContext.m_vars = new Dictionary<string, object>();
            vars = vars.Replace("\"", "");
            string str1 = vars.Substring(3);
            char[] separator1 = new char[1] { ';' };
            foreach (string str2 in str1.Split(separator1, StringSplitOptions.RemoveEmptyEntries))
            {
                char[] separator2 = new char[1] { '=' };
                string[] strArray = str2.Split(separator2, StringSplitOptions.RemoveEmptyEntries);
                if (strArray.Length == 2)
                    ProgramContext.m_vars.Add(strArray[0], (object)strArray[1]);
            }
        }

        public static MyMaterialConfiguration GetMaterialByRef(string materialRef)
        {
            foreach (MyMaterialsLib materialLib in ProgramContext.m_materialLibs)
            {
                MyMaterialConfiguration materialConfiguration = Array.Find<MyMaterialConfiguration>(materialLib.Materials, (Predicate<MyMaterialConfiguration>)(e => e.Name == materialRef));
                if (materialConfiguration != null)
                    return materialConfiguration;
            }
            return (MyMaterialConfiguration)null;
        }

        public static MyMaterialConfiguration LoadMaterialByRef(
          string materialRef)
        {
            MyMaterialConfiguration materialByRef = ProgramContext.GetMaterialByRef(materialRef);
            if (materialByRef == null)
                ProgramContext.m_buildLogger.LogMessage(MessageType.Error, "Referenced material: " + materialRef + " was not found in material libraries", "");
            return materialByRef;
        }

        public static void LoadMaterialLibs()
        {
            ProgramContext.m_materialLibs = new List<MyMaterialsLib>();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(MyMaterialsLib));
            new XmlSerializerNamespaces().Add(string.Empty, string.Empty);
            if (Directory.Exists(ProgramContext.m_materialsPath))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(ProgramContext.m_materialsPath))
                    {
                        string xmlFile = file;
                        if (xmlFile.EndsWith(".xml"))
                        {
                            ProgramContext.m_buildLogger.LogMessage(MessageType.Info, "Materialslib", xmlFile);
                            using (FileStream fileStream = File.Open(xmlFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                try
                                {
                                    lock (ProgramContext.m_materialLibs)
                                    {
                                        if (ProgramContext.m_materialLibs.Find((Predicate<MyMaterialsLib>)(x => x.FilePath == xmlFile)) == null)
                                        {
                                            MyMaterialsLib myMaterialsLib = (MyMaterialsLib)xmlSerializer.Deserialize((Stream)fileStream);
                                            myMaterialsLib.FilePath = xmlFile;
                                            ProgramContext.m_materialLibs.Add(myMaterialsLib);
                                        }
                                    }
                                }
                                catch
                                {
                                    ProgramContext.m_buildLogger.LogMessage(MessageType.Error, "This xml library: " + xmlFile + " couldn't been loaded. Probably wrong XML format.", "");
                                }
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    ProgramContext.m_buildLogger.LogMessage(MessageType.Error, "Libs in " + ProgramContext.m_materialsPath + " couldn't been loaded.. Wrong material library path?", "");
                }
            }

            Trace.Assert(ProgramContext.m_materialLibs != null, "No material libraries were loaded from path: " + ProgramContext.m_materialsPath + " !");
            if (ProgramContext.m_materialLibs == null)
                return;

            ProgramContext.m_buildLogger.LogMessage(MessageType.Info, "Material libraries were successfully loaded from path: " + ProgramContext.m_materialsPath, "Material Libraries");
            ProgramContext.CheckForMaterialDuplicates();
        }

        public static void CheckForMaterialDuplicates()
        {
            foreach (MyMaterialsLib materialLib1 in ProgramContext.m_materialLibs)
            {
                foreach (MyMaterialConfiguration material1 in materialLib1.Materials)
                {
                    foreach (MyMaterialsLib materialLib2 in ProgramContext.m_materialLibs)
                    {
                        foreach (MyMaterialConfiguration material2 in materialLib2.Materials)
                            Trace.Assert((material1.Equals((object)material2) ? 1 : (material1.Name != material2.Name ? 1 : 0)) != 0, "Material: " + material1.Name + " from library: " + materialLib1.FilePath + " is duplicated in: " + materialLib2.FilePath);
                    }
                }
            }
        }

        private struct ItemInfo
        {
            public int Index;
            public string Name;
            public string Path;
            public MyModelConfiguration Configuration;
        }
    }
}
