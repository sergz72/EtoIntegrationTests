using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;
using EtoIntegrationTests.Model;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests;

public class Service: IService
{
  private static readonly Dictionary<string, IPageBuilder> Builders = new()
  {
    { "console", new ConsolePageBuilder() },
    { "kafka", new KafkaPageBuilder() },
    { "cassandra", new CassandraPageBuilder() }
  };

  public static bool IsValidWindowType(string windowType)
  {
    return Builders.ContainsKey(windowType);
  }

  public static bool IsConsoleWindow(ScriptWindow window) => window.Type == "console";

  public readonly string Name;
  public readonly string UrlForTests;

  public ItemStatus Status { get; private set; }
  
  public IStatusChange? StatusChangeHandler { get; set; }
  
  public readonly List<TabPage> Pages;

  private readonly ServiceScript _sscript;
  private readonly ConsoleLogger _logger;
  private volatile Process? _process;
  private volatile bool _stopRequest;
  private string? _batchFileName;
  private readonly int _startDelay;

  public Service(string serviceName, ServiceScript sscript, bool disabled, string urlForTests)
  {
    Name = serviceName;
    _sscript = sscript;
    Status = disabled ? ItemStatus.Disabled : ItemStatus.Stopped;
    _logger = new ConsoleLogger();
    _startDelay = sscript.StartDelay * 1000;
    Pages = BuildPages(sscript.ScriptWindows, _logger);
    UrlForTests = urlForTests;
  }

  private List<TabPage> BuildPages(Dictionary<string, ScriptWindow> windows, ConsoleLogger logger)
  {
    return windows.Select(window => 
      Builders[window.Value.Type].BuildPage(window.Key, window.Value.Parameters, logger)).ToList();
  }

  public static List<Service> Build(string serviceName, Dictionary<string, List<Service>> serviceMap, Script script)
  {
    if (serviceMap.ContainsKey(serviceName))
    {
      return serviceMap[serviceName];
    }

    var service = script.LocalServices[serviceName];
    var services = service.Scripts
      .Select(s => new Service(s.Key, s.Value, service.Disabled, service.UrlForTests)).ToList();
    serviceMap[serviceName] = services;
    return services;
  }

  public void Start()
  {
    _stopRequest = false;
    if (Status == ItemStatus.Disabled)
      return;

    Status = ItemStatus.Starting;
    StatusChangeHandler?.StatusChanged();
    
    Task.Run(() =>
    {
      if (_stopRequest)
      {
        Status = ItemStatus.Stopped;
        StatusChangeHandler?.StatusChanged();
        return;
      }

      foreach (var port in _sscript.WaitForPorts)
      {
        while (!WaitForPort(port))
        {
          if (_stopRequest)
          {
            Status = ItemStatus.Stopped;
            StatusChangeHandler?.StatusChanged();
            return;
          }
          Thread.Sleep(500);
        }
      }
      if (_startDelay > 0)
        Thread.Sleep(_startDelay);
      if (_stopRequest)
      {
        Status = ItemStatus.Stopped;
        StatusChangeHandler?.StatusChanged();
        return;
      }
      var p = new Process();
      if (_sscript.Commands.Count > 1)
      {
        _batchFileName = _sscript.CreateBatchFile();
        p.StartInfo.FileName = _batchFileName;
      }
      else
      {
        _batchFileName = null;
        var parts = _sscript.Commands[0].Split(' ');
        if (parts.Length == 0)
        {
          _logger.AddErrorLine("Empty command line");
          return;
        }

        var absolutePath = Path.Combine(_sscript.Workdir, parts[0]);
        p.StartInfo.FileName = new FileInfo(absolutePath).Exists ? absolutePath : parts[0];
        p.StartInfo.Arguments = string.Join(' ', parts.Skip(1));
      }

      p.StartInfo.WorkingDirectory = _sscript.Workdir;
      p.StartInfo.CreateNoWindow = true;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;
      BuildEnvironment(p.StartInfo);
      p.Exited += delegate
      {
        _process = null;
        Status = ItemStatus.Stopped;
        StatusChangeHandler?.StatusChanged();
      };
      p.OutputDataReceived += (_, data) => _logger.AddLine(data.Data);
      p.ErrorDataReceived += (_, data) => _logger.AddStderrLine(data.Data);
      p.EnableRaisingEvents = true;
      try
      {
        if (_stopRequest)
        {
          Status = ItemStatus.Stopped;
          StatusChangeHandler?.StatusChanged();
          return;
        }
        p.Start();
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
        _process = p;
        Status = ItemStatus.Started;
        StatusChangeHandler?.StatusChanged();
      }
      catch (Exception e)
      {
        _logger.AddErrorLine(e.Message);
        Stop();
      }
    });
  }

  private void BuildEnvironment(ProcessStartInfo pStartInfo)
  {
    if (_sscript.EnvFile.Length > 0)
    {
      foreach (var line in File.ReadLines(_sscript.EnvFile))
      {
        var trimmed = line.Trim().Replace("'", "");
        if (trimmed.Length == 0 || trimmed.StartsWith("#"))
          continue;
        var idx = trimmed.IndexOf('=');
        if (idx > 0)
        {
          var name = trimmed.Substring(0, idx);
          var value = trimmed.Substring(idx + 1);
          pStartInfo.Environment.Add(name, value);
        }
      }
    }
  }

  private bool WaitForPort(int port)
  {
    var properties = IPGlobalProperties.GetIPGlobalProperties();
    return properties.GetActiveTcpListeners().Any(endpoint => endpoint.Port == port);
  }

  public void Stop()
  {
    _stopRequest = true;
    _process?.Kill(true);
    _process = null;
    if (_batchFileName != null)
    {
      File.Delete(_batchFileName);
      _batchFileName = "";
    }
    _logger.Clear();
    Status = ItemStatus.Stopped;
    StatusChangeHandler?.StatusChanged();
  }

  public List<string> GetLogs()
  {
    return _logger.GetLogs();
  }

  public void ClearLogs()
  {
    Application.Instance.Invoke(_logger.Clear);
  }

  public string GetUrlForTests()
  {
    return UrlForTests;
  }
}

public interface IStatusChange
{
  void StatusChanged();
}

interface IPageBuilder
{
  TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger);
}

class ConsolePageBuilder : IPageBuilder
{
  public TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger)
  {
    return new TabPage{ Content = logger, Text = pageName };
  }
}