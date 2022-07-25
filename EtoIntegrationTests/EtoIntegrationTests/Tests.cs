using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Eto.Forms;
using EtoIntegrationTests.Interfaces;
using EtoIntegrationTests.Model;

namespace EtoIntegrationTests;

public class Tests: StackLayout, ITestLogger
{
  private readonly TestList _testsList;
  private readonly ConsoleLogger _testsResults;
  private readonly Button _runAllButton;
  
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
    var runButton = new Button
    {
      Text = "Run test",
      Enabled = false
    };
    runButton.Click += RunButtonOnClick;

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
            Control = runButton,
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

  public void ShowTests(string? folder, Parameters parameters, Dictionary<string, IService> services)
  {
    if (folder != null)
    {
      foreach (var file in Directory.GetFiles(folder, "*.dll"))
      {
        try
        {
          var alc = new AssemblyLoadContext("test", true);

          Assembly a = alc.LoadFromAssemblyPath(Path.GetFullPath(file));
          foreach (var type in a.GetTypes())
          {
            if (type.GetInterfaces().Contains(typeof(ITests)))
            {
              var t = Activator.CreateInstance(type) as ITests;
              var tests = t.Init(parameters, services, this);
            }
          }
          alc.Unload();
        }
        catch (Exception e)
        {
          _testsResults.AddErrorLine(e.Message);
        }
      }
    }
  }
  
  private void TestsListOnSelectedItemChanged(object? sender, EventArgs e)
  {
  }

  private void RunButtonOnClick(object? sender, EventArgs e)
  {
  }

  private void RunAllButtonOnClick(object? sender, EventArgs e)
  {
  }

  public void Log(string line)
  {
    _testsResults.AddLine(line);
  }
}