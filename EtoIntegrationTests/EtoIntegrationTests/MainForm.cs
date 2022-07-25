using System;
using Eto.Forms;
using Eto.Drawing;

namespace EtoIntegrationTests
{
  public class MainForm : Form
  {
    private Scripts _scriptsView;
    private ButtonToolItem _startButton, _stopButton, _reloadScriptsButton, _clearConsoleButton;
    private StackLayout _panel1;
    private Label _scriptsLabel, _tasksLabel;
    private Tasks _tasks;
    private TabControl _actionsPanel;
    private Tests _tests;

    private void InitToolbar()
    {
      _reloadScriptsButton = new ButtonToolItem
      {
        Text = "Reload scripts"
      };
      _reloadScriptsButton.Click += delegate { ReloadScripts(); };
      _startButton = new ButtonToolItem
      {
        Text = "Start",
        Enabled = false
      };
      _startButton.Click += StartButtonOnClick;
      _stopButton = new ButtonToolItem
      {
        Text = "Stop",
        Enabled = false
      };
      _stopButton.Click += StopButtonOnClick;
      _clearConsoleButton = new ButtonToolItem
      {
        Text = "Clear consoles",
        Enabled = false
      };
      _clearConsoleButton.Click += ClearConsoleButtonOnClick;

      ToolBar = new ToolBar
      {
        Items =
        {
          _reloadScriptsButton,
          new SeparatorToolItem(),
          _startButton,
          new SeparatorToolItem(),
          _stopButton,
          new SeparatorToolItem(),
          _clearConsoleButton
        }
      };
    }

    private void InitPanel1()
    {
      _scriptsView = new Scripts();
      _scriptsView.SelectedItemChanged += ScriptsViewOnSelectedItemChanged;
      _scriptsLabel = new Label
      {
        Text = "Scripts"
      };

      _panel1 = new StackLayout
      {
        Orientation = Orientation.Vertical,
        Items =
        {
          _scriptsLabel,
          _scriptsView
        },
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
      };
      _panel1.SizeChanged += (_, _) =>
      {
        if (_scriptsView.Height != _panel1.Height - _scriptsLabel.Height)
          _scriptsView.Height = _panel1.Height - _scriptsLabel.Height;
      };
    }

    private StackLayout InitTasksPanel()
    {
      _tasks = new Tasks();
      _tasks.SelectedItemChanged += TasksOnSelectedIndexChanged;
      _tasksLabel = new Label
      {
        Text = "Tasks"
      };
      var tasksPanel = new StackLayout
      {
        Orientation = Orientation.Vertical,
        Items =
        {
          _tasksLabel,
          _tasks
        },
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
      };
      tasksPanel.SizeChanged += (_, _) =>
      {
        if (_tasks.Height != tasksPanel.Height - _tasksLabel.Height)
          _tasks.Height = tasksPanel.Height - _tasksLabel.Height;
      };

      return tasksPanel;
    }

    private void InitActionsPanel()
    {
      _actionsPanel = new TabControl();
    }

#pragma warning disable CS8618
    public MainForm()
    {
      Title = "Integration tests";
      MinimumSize = new Size(800, 400);
      
      InitToolbar();
      InitPanel1();
      InitActionsPanel();

      var panel2 = new Splitter
      {
        Orientation = Orientation.Horizontal,
        Panel1 = InitTasksPanel(),
        Panel2 = _actionsPanel,
        BackgroundColor = Colors.White,
        Panel1MinimumSize = 150
      };

      _tests = new Tests();
      
      Content = new StackLayout
      {
        Orientation = Orientation.Vertical,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Items = {
          new StackLayoutItem
          {
            Control = new Splitter
            {
              Orientation = Orientation.Horizontal,
              Panel1 = _panel1,
              Panel2 = panel2,
              BackgroundColor = Colors.White,
              Panel1MinimumSize = 150
            },
            Expand = true
          },
          new StackLayoutItem
          {
            Control = _tests,
            Expand = true
          }
        }
      };

      ReloadScripts();
    }
#pragma warning restore CS8618

    private void ReloadScripts()
    {
      try
      {
        _scriptsView.Reload();
      }
      catch (Exception e)
      {
        MessageBox.Show(this, e.Message, "Error");
      }
    }
    private void ScriptsViewOnSelectedItemChanged(object? sender, EventArgs e)
    {
      var item = _scriptsView.SelectedItem as ScriptsTreeItem;
      _tasks.ShowServices(item?.GetServices());
      if (item is not { Expandable: false })
      {
        _stopButton.Enabled = false;
        _startButton.Enabled = false;
      }
      else
      {
        if (item.IsStarted)
        {
          _stopButton.Enabled = true;
          _startButton.Enabled = false;
        }
        else
        {
          _stopButton.Enabled = false;
          _startButton.Enabled = true;
        }
      }
    }

    private void TasksOnSelectedIndexChanged(object? sender, EventArgs e)
    {
      var item = _tasks.SelectedItem as TasksItem;
      _actionsPanel.Pages.Clear();
      if (item is not { CanBeStarted: true })
      {
        _stopButton.Enabled = false;
        _startButton.Enabled = false;
      }
      else
      {
        item.Pages.ForEach(page => _actionsPanel.Pages.Add(page));
        
        if (item.IsStarted)
        {
          _stopButton.Enabled = true;
          _startButton.Enabled = false;
        }
        else
        {
          _stopButton.Enabled = false;
          _startButton.Enabled = true;
        }
      }
    }

    private void StartServices(IStartable? item)
    {
      try
      {
        item?.Start();
      }
      catch (Exception exception)
      {
        MessageBox.Show(this, exception.Message, "Error");
      }
    }

    private void StopServices(IStartable? item)
    {
      try
      {
        item?.Stop();
      }
      catch (Exception exception)
      {
        MessageBox.Show(this, exception.Message, "Error");
      }
    }

    private void StartButtonOnClick(object? sender, EventArgs e)
    {
      if (_tasks.HasFocus)
      {
        StartServices(_tasks.SelectedItem as TasksItem);
      }
      else
      {
        StartServices(_scriptsView.SelectedItem as ScriptsTreeItem);
      }
    }
    
    private void StopButtonOnClick(object? sender, EventArgs e)
    {
      if (_tasks.HasFocus)
      {
        StopServices(_tasks.SelectedItem as TasksItem);
      }
      else
      {
        StopServices(_scriptsView.SelectedItem as ScriptsTreeItem);
      }
    }
    
    private void ClearConsoleButtonOnClick(object? sender, EventArgs e)
    {
    }
  }
}