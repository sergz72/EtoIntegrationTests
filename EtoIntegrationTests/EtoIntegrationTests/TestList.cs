using System;
using System.Collections.Generic;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests;

public class TestList: TreeGridView
{
  private readonly TestsDataStore _dataStore;

  public TestList()
  {
    ShowHeader = false;
    _dataStore = new TestsDataStore();
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

  public void RunAllTests(ConsoleLogger logger)
  {
    _dataStore.RunAllTests(logger);
    ReloadData();
  }
}

class TestsDataStore : ITreeGridStore<TestItem>
{
  private readonly List<TestItem> _items = new();
  public int Count => _items.Count;

  public TestItem this[int index] => _items[index];

  internal void RunAllTests(ConsoleLogger logger)
  {
    foreach (var item in _items)
    {
      item.Run(logger);
    }
  }
}

class TestItem : ITreeGridItem<TestItem>
{
  public bool Expanded { get; set; }
  public bool Expandable => Count > 0;
  public ITreeGridItem? Parent { get; set; }
  public int Count => 0;
  public string Text { get; }
  public bool? Status { get; private set; }
  public Image ItemImage => GetImage();

  private TestDelegate _delegate;

  private Image GetImage()
  {
    if (Status == null)
      return TasksItem.GrayImage;
    return Status.Value ? TasksItem.GreenImage : TasksItem.RedImage;
  }

  public TestItem this[int index] => throw new NotImplementedException();

  public TestItem(string name, TestDelegate d)
  {
    Text = name;
    _delegate = d;
  }

  public void Run(ConsoleLogger logger)
  {
    var result = _delegate.Invoke();
    Status = result.Success;
    if (!result.Success)
      logger.AddErrorLine(result.ErrorMessage);
  }
}

