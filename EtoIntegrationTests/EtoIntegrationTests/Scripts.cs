using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Forms;
using EtoIntegrationTests.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EtoIntegrationTests
{
  public class Scripts: TreeGridView
  {
    private readonly ScriptsDataStore _dataStore;
    public Scripts()
    {
      _dataStore = new ScriptsDataStore();
      DataStore = _dataStore;
      AllowEmptySelection = false;
      Columns.Add(new GridColumn
      {
        DataCell = new TextBoxCell { Binding = new DelegateBinding<ScriptsTreeItem, string>(r => r.Text) }
      });
    }

    public void Reload()
    {
      _dataStore.Clear();
      foreach (var script in Directory.GetFiles("scripts"))
      {
        _dataStore.Add(script);
      }
      ReloadData();
    }
  }

  class ScriptsDataStore : ITreeGridStore<ScriptsTreeItem>
  {
    private readonly List<ScriptsTreeItem> _items = new();
    public int Count => _items.Count;

    public ScriptsTreeItem this[int index] => _items[index];

    public void Clear()
    {
      _items.Clear();
    }

    public void Add(string scriptName)
    {
      _items.Add(new ScriptsTreeItem(new ScriptsHandler(scriptName), null));
    }
  }

  interface IScriptsTreeItemHandler
  {
    string GetText();
    List<ScriptsTreeItem> GetChildren(ScriptsTreeItem parent);
    Dictionary<string,List<Service>> GetServices();
  }

  class ScriptsHandler : IScriptsTreeItemHandler
  {
    private readonly string _name, _scriptName;
    private readonly Dictionary<string, List<Service>> _serviceMap = new();

    public ScriptsHandler(string scriptName)
    {
      _scriptName = scriptName;
      _name = Path.GetFileNameWithoutExtension(scriptName);
    }
    
    public string GetText()
    {
      return _name;
    }

    public List<ScriptsTreeItem> GetChildren(ScriptsTreeItem parent)
    {
      var input = new StringReader(File.ReadAllText(_scriptName));

      var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

      var script = deserializer.Deserialize<Script>(input);
      script.Validate();
      
      return script.ServiceSets.Select(set => 
        new ScriptsTreeItem(new ServiceSetHandler(set.Key, set.Value, _serviceMap, script), parent)).ToList();
    }

    public Dictionary<string, List<Service>> GetServices()
    {
      return new Dictionary<string, List<Service>>();
    }
  }

  class ServiceSetHandler : IScriptsTreeItemHandler
  {
    private readonly string _name;
    private readonly Dictionary<string, List<Service>> _services;
    
    public ServiceSetHandler(string name, ServiceSet set, Dictionary<string, List<Service>> serviceMap, Script script)
    {
      _name = name;
      _services = set.GetServices(script).Select(service => KeyValuePair.Create(service, Service.Build(service, serviceMap, script)))
        .ToDictionary(service => service.Key, service => service.Value);
    }

    public string GetText()
    {
      return _name;
    }

    public List<ScriptsTreeItem> GetChildren(ScriptsTreeItem parent)
    {
      return new List<ScriptsTreeItem>();
    }

    public Dictionary<string, List<Service>> GetServices()
    {
      return _services;
    }
  }
  
  class ScriptsTreeItem : ITreeGridItem<ScriptsTreeItem>, IStartable
  {
    public string Text => _handler.GetText();
    public bool Expanded { get; set; }
    public bool Expandable => _children.Count > 0;
    public ITreeGridItem? Parent { get; set; }

    public bool IsStarted { get; set; }
    
    private readonly List<ScriptsTreeItem> _children;
    private readonly IScriptsTreeItemHandler _handler;
  
    public ScriptsTreeItem(IScriptsTreeItemHandler handler, ScriptsTreeItem? parent)
    {
      _handler = handler;
      Parent = parent;
      _children = handler.GetChildren(this);
      Expanded = true;
    }

    public int Count => _children.Count;

    public ScriptsTreeItem this[int index] => _children[index];

    public Dictionary<string,List<Service>> GetServices()
    {
      return _handler.GetServices();
    }

    public void Start()
    {
      foreach (var service in _handler.GetServices().SelectMany(services => services.Value))
        service.Start();
      IsStarted = true;
    }

    public void Stop()
    {
      foreach (var service in _handler.GetServices().SelectMany(services => services.Value))
        service.Stop();
      IsStarted = false;
    }
  }

  internal interface IStartable
  {
    void Start();
    void Stop();
  }
}
