using System;
using System.Collections.Generic;
using System.Linq;

namespace MwmBuilder
{
  internal class MyProgramArgs
  {
    private Dictionary<string, string> m_args = new Dictionary<string, string>();
    private Dictionary<string, MyProgramArgs.CmdArg> m_registeredArgs = new Dictionary<string, MyProgramArgs.CmdArg>();

    public bool Empty
    {
      get
      {
        return this.m_args.Count == 0;
      }
    }

    public void RegisterArg(string command, string name, string defaultValue, string help)
    {
      this.m_registeredArgs.Add(command, new MyProgramArgs.CmdArg()
      {
        Command = command,
        Name = name,
        DefaultValue = defaultValue,
        Help = help
      });
    }

    public void Parse(string s)
    {
      this.Parse(s.Split(' '));
    }

    public void Parse(string[] args)
    {
      this.m_args.Clear();
      foreach (string str in args)
        this.ParseArg(str);
    }

    private void ParseArg(string arg)
    {
      string source = arg.TrimStart('/');
      string str = "true";
      int num = arg.IndexOf(':');
      if (num != -1)
      {
        source = arg.Substring(1, num - 1);
        str = arg.Substring(num + 1);
      }
      if (!string.IsNullOrEmpty(source) && source.All<char>(new Func<char, bool>(char.IsLetter)))
        this.m_args[source] = str;
      else
        Console.WriteLine("Couldn't parse argument: " + arg);
    }

    public void SetArg(string arg, string value)
    {
      if (this.m_args.GetValueOrDefault<string, string>(arg) != null)
        this.m_args[arg] = value;
      else
        this.m_args.Add(arg, value);
    }

    public string GetValue(string command)
    {
      if (!this.m_registeredArgs.ContainsKey(command))
        return (string) null;
      return this.m_args.ContainsKey(command) ? this.m_args[command] : this.m_registeredArgs[command].DefaultValue;
    }

    public void WriteHelp()
    {
      Console.WriteLine("Invalid usage: " + Environment.CommandLine);
      string str = "";
      foreach (KeyValuePair<string, MyProgramArgs.CmdArg> registeredArg in this.m_registeredArgs)
      {
        if (registeredArg.Value.Help != null)
        {
          str += "[/";
          str += registeredArg.Value.Command;
          if (registeredArg.Value.Name != null)
            str = str + ":" + registeredArg.Value.Name;
          str += "] ";
        }
      }
      Console.WriteLine("Usage: MwmBuilder.exe " + str);
      Console.WriteLine();
      foreach (KeyValuePair<string, MyProgramArgs.CmdArg> registeredArg in this.m_registeredArgs)
      {
        if (registeredArg.Value.Help != null)
        {
          if (registeredArg.Value.Name != null)
            Console.WriteLine("    " + registeredArg.Value.Name + " - " + registeredArg.Value.Help);
          else
            Console.WriteLine("    /" + registeredArg.Value.Command + " - " + registeredArg.Value.Help);
        }
      }
    }

    private struct CmdArg
    {
      public string Command;
      public string Name;
      public string DefaultValue;
      public string Help;
    }
  }
}
