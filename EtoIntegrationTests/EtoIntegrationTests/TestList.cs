using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
  
  public void ShowTests(Dictionary<string, TestDelegate> tests)
  {
    _dataStore.Clear();
    foreach (var test in tests)
      _dataStore.Add(test.Key);
    ReloadData();
  }
  
  public void RunAllTests(Dictionary<string, TestDelegate> tests, ConsoleLogger logger)
  {
    Task.Run(() =>
    {
      _dataStore.RunAllTests(tests, logger, this);
      logger.AddLine("Done.");
    });
  }
  
  public void RunSelectedTests(Dictionary<string, TestDelegate> tests, ConsoleLogger logger)
  {
    Task.Run(() =>
    {
      foreach (var item in SelectedItems)
      {
        var ti = item as TestItem;
        ti?.Run(tests[ti.Text], logger, this);
      }
      logger.AddLine("Done.");
    });
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
  
  internal void RunAllTests(Dictionary<string, TestDelegate> tests, ConsoleLogger logger, TestList testList)
  {
    foreach (var item in _items)
    {
      item.Run(tests[item.Text], logger, testList);
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

  public void Run(TestDelegate d, ConsoleLogger logger, TestList testList)
  {
    Status = TestStatus.Started;
    Application.Instance.Invoke(testList.ReloadData);
    try
    {
      var result = d.Invoke();
      Status = result.Success ? TestStatus.Success : TestStatus.Failure;
      if (!result.Success)
        logger.AddErrorLine(result.ErrorMessage);
    }
    catch (Exception e)
    {
      Status = TestStatus.Failure;
      logger.AddErrorLine(e.Message);
    }
    Application.Instance.Invoke(testList.ReloadData);
  }
}

public enum TestStatus
{
  NotStarted,
  Started,
  Success,
  Failure
}