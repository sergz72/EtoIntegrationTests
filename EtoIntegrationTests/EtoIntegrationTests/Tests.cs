using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Eto.Forms;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests;

public class Tests: StackLayout, ITestLogger
{
  private readonly TestList _testsList;
  private readonly ConsoleLogger _testsResults;
  private readonly Button _runAllButton, _runButton;
  private string? _folder;
  private ITestParameters? _parameters;
  private Dictionary<string, IService>? _services;

  public Tests()
  {
    _testsList = new TestList();
    _testsList.Width = 300;
    _testsList.SelectedItemChanged += TestsListOnSelectedItemChanged;
    _testsResults = new ConsoleLogger();
    _runAllButton = new Button
    {
      Text = "Run all tests",
      Enabled = false
    };
    _runAllButton.Click += RunAllButtonOnClick;
    _runButton = new Button
    {
      Text = "Run tests",
      Enabled = false
    };
    _runButton.Click += RunButtonOnClick;

    Orientation = Orientation.Vertical;
    HorizontalContentAlignment = HorizontalAlignment.Stretch;
    Items.Add(new StackLayoutItem
    {
      Control = new StackLayout
      {
        Orientation = Orientation.Horizontal,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Items =
        {
          new StackLayoutItem
          {
            Control = _runAllButton,
            Expand = true
          },
          new StackLayoutItem
          {
            Control = _runButton,
            Expand = true
          }
        }
      }
    });
    Items.Add(new StackLayoutItem
    {
      Control = new StackLayout
      {
        Orientation = Orientation.Horizontal,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Items =
        {
          new StackLayoutItem
          {
            Control = _testsList
          },
          new StackLayoutItem
          {
            Control = _testsResults,
            Expand = true
          }
        }
      }
    });
    SizeChanged += (_, _) =>
    {
      if (_testsList.Height != Content.Height - _runAllButton.Height)
      {
        _testsList.Height = Content.Height - _runAllButton.Height;
        _testsResults.Height = Content.Height - _runAllButton.Height;
      }
    };
  }

  private void TestsHandler(Action<Dictionary<string, TestDelegate>> handler)
  {
    if (_folder == null)
      return;
    var testsFile = Directory.GetFiles(_folder, "*.dll").FirstOrDefault();
    if (testsFile == null)
      return;
    try
    {
      var alc = new AssemblyLoadContext("test", true);

      Assembly a = alc.LoadFromAssemblyPath(Path.GetFullPath(testsFile));
      foreach (var type in a.GetTypes())
      {
        if (type.GetInterfaces().Contains(typeof(ITests)))
        {
          var t = Activator.CreateInstance(type) as ITests;
          var tests = t?.Init(_parameters, _services, this);
          if (tests != null)
            handler(tests);
        }
      }
      alc.Unload();
    }
    catch (Exception e)
    {
      _testsResults.AddErrorLine(e.Message);
    }
  }
  
  public void ShowTests(string? folder, ITestParameters? parameters, Dictionary<string, IService>? services)
  {
    _folder = folder;
    _parameters = parameters;
    _services = services;
    TestsHandler(tests => _testsList.ShowTests(tests));
    _runAllButton.Enabled = _testsList.IsNotEmpty();
    _runButton.Enabled = false;
  }
  
  private void TestsListOnSelectedItemChanged(object? sender, EventArgs e)
  {
    _runButton.Enabled = _testsList.SelectedItems.Any();
  }

  private void RunButtonOnClick(object? sender, EventArgs e)
  {
    TestsHandler(tests => _testsList.RunSelectedTests(tests, _testsResults));
  }

  private void RunAllButtonOnClick(object? sender, EventArgs e)
  {
    TestsHandler(tests => _testsList.RunAllTests(tests, _testsResults));
  }

  public void Log(string line)
  {
    _testsResults.AddLine(line);
  }

  public ITestParameters? GetParameters()
  {
    return _parameters;
  }

  public Dictionary<string, IService>? GetServices()
  {
    return _services;
  }
}