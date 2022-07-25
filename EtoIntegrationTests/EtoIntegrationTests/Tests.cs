using System;
using Eto.Forms;

namespace EtoIntegrationTests;

public class Tests: StackLayout
{
  private readonly ListBox _testsList;
  private readonly TreeGridView _testsResults;
  private readonly Button _runAllButton;
  
  public Tests()
  {
    _testsList = new ListBox();
    _testsList.Width = 300;
    _testsResults = new TreeGridView();
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

  private void RunButtonOnClick(object? sender, EventArgs e)
  {
  }

  private void RunAllButtonOnClick(object? sender, EventArgs e)
  {
  }
}