using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace EtoIntegrationTests;

public class ConsoleLogger: TreeGridView
{
  private readonly LogDataStore _dataStore;
  
  public ConsoleLogger()
  {
    _dataStore = new LogDataStore();
    DataStore = _dataStore;
    Columns.Add(new GridColumn
    {
      DataCell = new ImageTextCell
      {
        TextBinding = new DelegateBinding<LogItem, string>(r => r.Text),
        ImageBinding = new DelegateBinding<LogItem, Image>(r => r.ItemImage)
      }
    });
  }

  public void Clear()
  {
    _dataStore.Clear();
    ReloadData();
  }

  public void AddLine(string? line)
  {
    if (line != null && line.Trim().Length > 0)
    {
      _dataStore.Add(line);
      Application.Instance.Invoke(ReloadData);
    }
  }

  public void AddStderrLine(string? line)
  {
    if (line != null && line.Trim().Length > 0)
    {
      _dataStore.AddStderr(line);
      Application.Instance.Invoke(ReloadData);
    }
  }

  public void AddErrorLine(string? line)
  {
    if (line != null && line.Trim().Length > 0)
    {
      _dataStore.AddError(line);
      Application.Instance.Invoke(ReloadData);
    }
  }
}

class LogDataStore : ITreeGridStore<LogItem>
{
  private readonly List<LogItem> _items = new();
  public int Count => _items.Count;

  public LogItem this[int index] => _items[index];
  
  public void Clear()
  {
    _items.Clear();
  }

  public void Add(string line)
  {
    _items.Add(new LogItem(line, false, false));
  }

  public void AddError(string line)
  {
    _items.Add(new LogItem(line, true, false));
  }

  public void AddStderr(string line)
  {
    _items.Add(new LogItem(line, false, true));
  }

}

class LogItem : ITreeGridItem<LogItem>
{
  public bool Expanded { get; set; }
  public bool Expandable => false;
  public ITreeGridItem? Parent { get; set; }
  public int Count => 0;
  public string Text { get; }

  public Image ItemImage { get; }

  public LogItem this[int index] => throw new System.NotImplementedException();

  public LogItem(string line, bool isError, bool isStderr)
  {
    Text = line;
    ItemImage = GetLineImage(isError, isStderr);
  }

  private Image GetLineImage(bool isError, bool isStderr)
  {
    if (isError)
      return TasksItem.RedImage;
    if (isStderr)
      return TasksItem.YellowImage;
    return TasksItem.GrayImage;
  }
}
