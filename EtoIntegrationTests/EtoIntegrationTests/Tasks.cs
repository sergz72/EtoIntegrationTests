using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace EtoIntegrationTests;

public class Tasks: TreeGridView, IStatusChange
{
  private readonly TasksDataStore _dataStore;
  
  public Tasks()
  {
    _dataStore = new TasksDataStore();
    DataStore = _dataStore;
    Columns.Add(new GridColumn
    {
      DataCell = new ImageTextCell
      {
        TextBinding = new DelegateBinding<TasksItem, string>(r => r.Text),
        ImageBinding = new DelegateBinding<TasksItem, Image>(r => r.ItemImage)
      }
    });
  }

  public void ShowServices(Dictionary<string, List<Service>>? services)
  {
    _dataStore.Clear();
    if (services != null)
    {
      foreach (var service in services)
      {
        _dataStore.Add(service.Key, service.Value);
        foreach (var s in service.Value)
          s.StatusChangeHandler = this;
      }
    }
    ReloadData();
  }

  public void StatusChanged()
  {
    Application.Instance.Invoke(() =>
    {
      ReloadData();
      OnSelectedItemChanged(null);
    });
  }
}

class TasksDataStore : ITreeGridStore<TasksItem>
{
  private readonly List<TasksItem> _items = new();
  public int Count => _items.Count;

  public TasksItem this[int index] => _items[index];
  
  public void Clear()
  {
    _items.Clear();
  }

  public void Add(string name, List<Service> services)
  {
    _items.Add(new TasksItem(name, services));
  }
}

class TasksItem : ITreeGridItem<TasksItem>, IStartable
{
  internal static readonly Image GreenImage = BuildBitmap(Colors.Green);
  internal static readonly Image RedImage = BuildBitmap(Colors.Red);
  internal static readonly Image GrayImage = BuildBitmap(Colors.Gray);
  internal static readonly Image YellowImage = BuildBitmap(Colors.Yellow);
  internal static readonly Image IndigoImage = BuildBitmap(Colors.Indigo);

  private static Image BuildBitmap(Color color)
  {
    return new Bitmap(16, 16, PixelFormat.Format32bppRgb, Enumerable.Repeat(color, 256));
  }

  public bool Expanded { get; set; }
  public bool Expandable => Count > 0;
  public ITreeGridItem? Parent { get; set; }
  public int Count => _services?.Count ?? 0;
  public string Text { get; }
  public Image ItemImage => GetServiceImage();

  public TasksItem this[int index] => _services![index];

  public bool CanBeStarted => _service?.CanBeStarted ?? _services![0].CanBeStarted;
  public bool IsStarted => _service?.IsStarted ?? _isStarted;

  public List<TabPage> Pages => _service?.Pages ?? new List<TabPage>();

  private readonly List<TasksItem>? _services;
  private readonly Service? _service;
  private bool _isStarted;
  
  public TasksItem(string name, List<Service> services)
  {
    Text = name;
    _services = services.Select(service => new TasksItem(service)).ToList();
    _isStarted = false;
    Expanded = true;
  }

  public TasksItem(Service service)
  {
    Text = service.Name;
    _service = service;
    _isStarted = false;
  }

  private Image GetServiceImage()
  {
    if (_service != null)
    {
      if (!_service.CanBeStarted)
        return GrayImage;
      return _service.IsStarted ? GreenImage : RedImage;
    }

    if (!_services![0].CanBeStarted)
      return GrayImage;
    return _isStarted ? GreenImage : RedImage;
  }

  public void Start()
  {
    _service?.Start();
    if (_services != null)
    {
      foreach (var service in _services)
        service.Start();
    }
  }

  public void Stop()
  {
    _service?.Stop();
    if (_services != null)
    {
      foreach (var service in _services)
        service.Stop();
    }
  }
}
