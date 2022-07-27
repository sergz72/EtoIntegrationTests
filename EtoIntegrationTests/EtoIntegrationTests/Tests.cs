using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Eto.Forms;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests;

public class Tests: StackLayout, ITestLogger
{
  private readonly TestList _testsList;
  private readonly ConsoleLogger _testsResults;
  private readonly Button _runAllButton, _runButton;
  private readonly string _testRunnerFileName;
  private string? _folder;
  private ITestParameters? _parameters;
  private Dictionary<string, IService>? _services;

  public Tests(string testRunnerFileName)
  {
    _testRunnerFileName = testRunnerFileName;
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

  public void ShowTests(string? folder, ITestParameters? parameters, Dictionary<string, IService>? services)
  {
    if (folder == null || parameters == null || services == null || services.Count == 0)
    {
      _testsList.ShowTests(new List<string>());
      return;
    }
    
    _folder = folder;
    _parameters = parameters;
    _services = services;
    try
    {
      var output = TestRunnerListTests();
      _testsList.ShowTests(output);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
    _runAllButton.Enabled = _testsList.IsNotEmpty();
    _runButton.Enabled = false;
  }

  private List<string> TestRunnerListTests()
  {
    var p = new Process();
    p.StartInfo.FileName = _testRunnerFileName;
    p.StartInfo.Arguments = _folder + " list";
    p.StartInfo.CreateNoWindow = true;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.RedirectStandardError = true;
    p.Start();
    var lines = p.StandardOutput.ReadToEnd().Split("\n")
      .Select(line => line.Replace("\r", "")).Where(line => line.Length > 0).ToList();
    var errors = p.StandardError.ReadToEnd();
    if (errors.Length > 0)
      throw new InvalidDataException(errors);
    p.WaitForExit();
    return lines;
  }
  
  private void TestsListOnSelectedItemChanged(object? sender, EventArgs e)
  {
    _runButton.Enabled = _testsList.SelectedItems.Any();
  }

  private void RunButtonOnClick(object? sender, EventArgs e)
  {
    //TestsHandler(tests => _testsList.RunSelectedTests(tests, _testsResults));
  }

  private void RunAllButtonOnClick(object? sender, EventArgs e)
  {
    //TestsHandler(tests => _testsList.RunAllTests(tests, _testsResults));
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