using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EtoIntegrationTests.Interfaces;
using YamlDotNet.Serialization;

namespace EtoIntegrationTests.Model;

public class Script
{
  [YamlMember(Alias = "service_sets", ApplyNamingConventions = false)]
  public Dictionary<string, ServiceSet> ServiceSets { get; set; }

  [YamlMember(Alias = "service_subsets", ApplyNamingConventions = false)]
  public Dictionary<string, List<string>> ServiceSubSets { get; set; }

  [YamlMember(Alias = "local_services", ApplyNamingConventions = false)]
  public Dictionary<string, YAMLLocalService> LocalServices { get; set; }

  [YamlMember(Alias = "test_parameters", ApplyNamingConventions = false)]
  public Parameters TestParameters { get; set; }

  public Script()
  {
    ServiceSets = new Dictionary<string, ServiceSet>();
    LocalServices = new Dictionary<string, YAMLLocalService>();
    ServiceSubSets = new Dictionary<string, List<string>>();
    TestParameters = new Parameters();
  }

  public void Validate()
  {
    ValidateServices();
    ValidateServiceSubsets();
    ValidateServiceSets();
  }

  private void ValidateServiceSets()
  {
    foreach (var set in ServiceSets)
    {
      set.Value.Validate(set.Key, this);
    }
  }

  private void ValidateServiceSubsets()
  {
    foreach (var subset in ServiceSubSets)
    {
      foreach (var service in subset.Value)
      {
        if (!LocalServices.ContainsKey(service))
        {
          ValidationException(subset.Key, "unknown service name in subset");
        }
      }
    }
  }

  private void ValidateServices()
  {
    foreach (var service in LocalServices)
    {
      service.Value.Validate(service.Key);
    }
  }

  public static void ValidationException(string serviceName, string message)
  {
    throw new InvalidDataException($"Validation exception in {serviceName}: {message}");
  }
}

public class ServiceSet
{
  public List<string> Services { get; set; }

  public List<string> Includes { get; set; }

  public ServiceSet()
  {
    Services = new List<string>();
    Includes = new List<string>();
  }

  public void Validate(string setKey, Script script)
  {
    foreach (var include in Includes)
    {
      if (!script.ServiceSubSets.ContainsKey(include))
      {
        Script.ValidationException(setKey, "unknown service subset in includes");
      }
    }

    foreach (var service in Services)
    {
      if (!script.LocalServices.ContainsKey(service))
      {
        Script.ValidationException(setKey, "unknown service name");
      }
    }
  }

  public IEnumerable<string> GetServices(Script script)
  {
    return Services.Union(ExpandIncludes(script.ServiceSubSets));
  }

  private IEnumerable<string> ExpandIncludes(Dictionary<string, List<string>> serviceSubsets)
  {
    return Includes.Select(include => serviceSubsets[include]).SelectMany(set => set);
  }
}

public class YAMLLocalService
{
  public Dictionary<string, ServiceScript> Scripts { get; set; }
  public bool Disabled { get; set; }
  
  [YamlMember(Alias = "url_for_tests", ApplyNamingConventions = false)]
  public string UrlForTests { get; set; }

  public YAMLLocalService()
  {
    Scripts = new Dictionary<string, ServiceScript>();
    UrlForTests = "";
  }

  internal void Validate(string serviceName)
  {
    foreach (var script in Scripts)
    {
      script.Value.Validate(serviceName);
    }
  }
}

public class ServiceScript
{
  public string Workdir { get; set; }
  public List<string> Commands { get; set; }
  [YamlMember(Alias = "windows", ApplyNamingConventions = false)]
  public Dictionary<string, ScriptWindow> ScriptWindows { get; set; }
  [YamlMember(Alias = "start_delay", ApplyNamingConventions = false)]
  public int StartDelay { get; set; }
  [YamlMember(Alias = "wait_for_ports", ApplyNamingConventions = false)]
  public List<int> WaitForPorts { get; set; }

  [YamlMember(Alias = "env_file", ApplyNamingConventions = false)]
  public string EnvFile { get; set; }

  public ServiceScript()
  {
    Workdir = "";
    Commands = new List<string>();
    ScriptWindows = new Dictionary<string, ScriptWindow>();
    WaitForPorts = new List<int>();
    EnvFile = "";
  }

  internal void Validate(string serviceName)
  {
    if (Workdir.Length == 0)
    {
      Script.ValidationException(serviceName, "missing or empty script working directory");
    }
    if (Commands.Count == 0)
    {
      Script.ValidationException(serviceName, "missing or empty script commands list");
    }
    if (ScriptWindows.Count == 0)
      Script.ValidationException(serviceName, "service script has no output windows");
    ValidateScriptWindows(serviceName);
  }

  private void ValidateScriptWindows(string serviceName)
  {
    var consolePresent = false;
    foreach (var window in ScriptWindows)
    {
      window.Value.Validate(serviceName);
      if (Service.IsConsoleWindow(window.Value))
        consolePresent = true;
    }
    if (!consolePresent)
      Script.ValidationException(serviceName, "console window must be present in the window list");
  }

  public string CreateBatchFile()
  {
    var fileName = Path.GetTempPath() + Guid.NewGuid() + ".bat";
    using var w = new StreamWriter(fileName, false);
    foreach (var line in Commands)
      w.WriteLine(line);
    return fileName;
  }
}

public class ScriptWindow
{
  public string Type { get; set; }
  public string? Parameters { get; set; }

  public ScriptWindow()
  {
    Type = "";
  }

  public void Validate(string serviceName)
  {
    if (Type.Length == 0)
    {
      Script.ValidationException(serviceName, "missing or empty script window type");
    }
    if (!Service.IsValidWindowType(Type))
      Script.ValidationException(serviceName, "unknown window type");
  }
}

public class Parameters: ITestParameters
{
  public KafkaParameters Kafka { get; set; }
  public CassandraParameters Cassandra { get; set; }

  public Parameters()
  {
    Kafka = new KafkaParameters();
    Cassandra = new CassandraParameters();
  }

  public IKafkaParameters GetKafkaParameters()
  {
    return Kafka;
  }

  public ICassandraParameters GetCassandraParameters()
  {
    return Cassandra;
  }
}

public class KafkaParameters: IKafkaParameters
{
  public string Host { get; set; }
  
  public Dictionary<string, KafkaTopicParameters> Topics { get; set; }

  public KafkaParameters()
  {
    Host = "";
    Topics = new Dictionary<string, KafkaTopicParameters>();
  }

  public string GetHost()
  {
    return Host;
  }

  public Dictionary<string, IKafkaTopicParameters> GetTopics()
  {
    return Topics.ToDictionary(topic => topic.Key, topic => topic.Value as IKafkaTopicParameters);
  }
}

public class KafkaTopicParameters: IKafkaTopicParameters
{
  public string Name { get; set; }
  public string Group { get; set; }

  public KafkaTopicParameters()
  {
    Name = "";
    Group = "";
  }

  public string GetName()
  {
    return Name;
  }

  public string GetGroup()
  {
    return Group;
  }
}

public class CassandraParameters: ICassandraParameters
{
  public string Host { get; set; }
  public int Port { get; set; }
  [YamlMember(Alias = "db_name", ApplyNamingConventions = false)]
  public string DbName { get; set; }

  public CassandraParameters()
  {
    Host = "";
    DbName = "";
  }

  public string GetHost()
  {
    return Host;
  }

  public int GetPort()
  {
    return Port;
  }

  public string GetDbName()
  {
    return DbName;
  }
}
