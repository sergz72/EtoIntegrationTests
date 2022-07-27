using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Forms;
using EtoIntegrationTests.Common;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests;

public class Tests: StackLayout, ITestLogger
{
  private readonly TestList _testsList;
  private readonly ConsoleLogger _testsResults;
  private readonly Button _runAllButton, _runButton;
  private readonly string _testRunnerFileName;
  private string? _folder;
  private Parameters? _parameters;
  private Dictionary<string, IService>? _services;
  private string _currentTest;

  public Tests(string testRunnerFileName)
  {
    _currentTest = "";
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

  public void ShowTests(string? folder, Parameters? parameters, Dictionary<string, IService>? services)
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
      _testsResults.AddErrorLine(e.Message);
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

  private void TestRunnerRunTests(List<string>? tests)
  {
    if (tests == null)
      _testsList.ResetAllTests();
    else
    {
      if (tests.Count == 0)
        return;
      _testsList.ResetTests(tests);
    }
    
    var testNames = tests == null ? "" : " " + string.Join(" ", tests);

    _runButton.Enabled = false;
    _runAllButton.Enabled = false;
    
    var p = new Process();
    p.StartInfo.FileName = _testRunnerFileName;
    p.StartInfo.Arguments = _folder + " run" + testNames;
    p.StartInfo.CreateNoWindow = true;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.RedirectStandardError = true;
    p.OutputDataReceived += (_, data) => ProcessLine(data.Data);
    p.ErrorDataReceived += (_, data) => ProcessStderrLine(data.Data);
    p.Start();
    p.BeginErrorReadLine();
    p.BeginOutputReadLine();
    Task.Run(() => { 
      p.WaitForExit();
      EnableButtons();
    });
  }

  private void ProcessStderrLine(string? data)
  {
    _testsResults.AddErrorLine(data);
  }

  private void ProcessLine(string? data)
  {
    if (data != null)
    {
      if (data == "##success")
        _testsList.Success(_currentTest);
      else if (data.StartsWith("##started "))
      {
        _currentTest = data[11..];
        _testsList.Started(_currentTest);
      }
      else if (data.StartsWith("##failure "))
      {
        var errorMessage = data[10..];
        _testsResults.AddErrorLine(errorMessage);
        _testsList.Failure(_currentTest);
      }
      else
        _testsResults.AddLine(data);
    }
  }

  private void EnableButtons()
  {
    Application.Instance.Invoke(() => _runAllButton.Enabled = _testsList.IsNotEmpty());
    Application.Instance.Invoke(() => _runButton.Enabled = _testsList.SelectedItems.Any());
  }
  
  private void TestsListOnSelectedItemChanged(object? sender, EventArgs e)
  {
    _runButton.Enabled = _testsList.SelectedItems.Any();
  }

  private void RunButtonOnClick(object? sender, EventArgs e)
  {
    TestRunnerRunTests(_testsList.GetSelectedTests());
  }

  private void RunAllButtonOnClick(object? sender, EventArgs e)
  {
    TestRunnerRunTests(null);
  }

  public void Log(string line)
  {
    _testsResults.AddLine(line);
  }

  public Parameters? GetParameters()
  {
    return _parameters;
  }

  public Dictionary<string, IService>? GetServices()
  {
    return _services;
  }
}