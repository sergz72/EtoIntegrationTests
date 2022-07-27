using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace EtoIntegrationTests;

public class TestList: TreeGridView
{
  private readonly TestsDataStore _dataStore;

  public TestList()
  {
    ShowHeader = false;
    AllowMultipleSelection = true;
    _dataStore = new TestsDataStore();
    DataStore = _dataStore;
    Columns.Add(new GridColumn
    {
      DataCell = new ImageTextCell
      {
        TextBinding = new DelegateBinding<TestItem, string>(r => r.Text),
        ImageBinding = new DelegateBinding<TestItem, Image>(r => r.ItemImage)
      }
    });
  }

  public bool IsNotEmpty()
  {
    return _dataStore.IsNotEmpty();
  }
  
  public void ShowTests(List<string> tests)
  {
    _dataStore.Clear();
    foreach (var test in tests)
      _dataStore.Add(test);
    ReloadData();
  }

  public void ResetAllTests()
  {
    _dataStore.ResetAllTests();
    ReloadData();
  }
  
  public void ResetTests(List<string> tests)
  {
    _dataStore.ResetTests(tests);
    ReloadData();
  }

  public List<string> GetSelectedTests()
  {
    return SelectedItems.Select(item => (item as TestItem)!.Text).ToList();
  }

  public void Success(string name)
  {
    _dataStore.Success(name);
    Application.Instance.Invoke(ReloadData);
  }

  public void Started(string name)
  {
    _dataStore.Started(name);
    Application.Instance.Invoke(ReloadData);
  }

  public void Failure(string name)
  {
    _dataStore.Failure(name);
    Application.Instance.Invoke(ReloadData);
  }
}

class TestsDataStore : ITreeGridStore<TestItem>
{
  private readonly List<TestItem> _items = new();
  public int Count => _items.Count;

  public TestItem this[int index] => _items[index];

  public void Clear()
  {
    _items.Clear();
  }

  public void Add(string testName)
  {
    _items.Add(new TestItem(testName));
  }

  public bool IsNotEmpty()
  {
    return _items.Count > 0;
  }
  
  public void ResetAllTests()
  {
    foreach (var item in _items)
      item.Reset();
  }
  
  public void ResetTests(List<string> tests)
  {
    foreach (var item in _items)
    {
      if (tests.Contains(item.Text))
        item.Reset();
    }
  }

  public void Success(string name)
  {
    foreach (var item in _items)
    {
      if (name == item.Text)
        item.Success();
    }
  }

  public void Failure(string name)
  {
    foreach (var item in _items)
    {
      if (name == item.Text)
        item.Failure();
    }
  }

  public void Started(string name)
  {
    foreach (var item in _items)
    {
      if (name == item.Text)
        item.Started();
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
  public TestStatus Status { get; private set; }
  public Image ItemImage => GetImage();

  private Image GetImage()
  {
    return Status switch
    {
      TestStatus.NotStarted => TasksItem.GrayImage,
      TestStatus.Started => TasksItem.YellowImage,
      TestStatus.Success => TasksItem.GreenImage,
      _ => TasksItem.RedImage
    };
  }

  public TestItem this[int index] => throw new NotImplementedException();

  public TestItem(string name)
  {
    Text = name;
  }

  public void Reset()
  {
    Status = TestStatus.NotStarted;
  }

  public void Success()
  {
    Status = TestStatus.Success;
  }
  
  public void Failure()
  {
    Status = TestStatus.Failure;
  }
  
  public void Started()
  {
    Status = TestStatus.Started;
  }
}

public enum TestStatus
{
  NotStarted,
  Started,
  Success,
  Failure
}