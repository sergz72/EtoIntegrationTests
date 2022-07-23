using System;
using Eto.Forms;
using Eto.Drawing;

namespace EtoIntegrationTests
{
  public class MainForm : Form
  {
    private Scripts scriptsView;
    private ButtonToolItem startButton, stopButton, reloadScriptsButton, clearConsoleButton;
    private StackLayout panel1;
    private Label scriptsLabel, tasksLabel;
    private Splitter panel2;
    private Tasks tasks;
    private TabControl actionsPanel;

    private void InitToolbar()
    {
      reloadScriptsButton = new ButtonToolItem
      {
        Text = "Reload scripts"
      };
      reloadScriptsButton.Click += delegate(object? sender, EventArgs args) { ReloadScripts(); };
      startButton = new ButtonToolItem
      {
        Text = "Start",
        Enabled = false
      };
      startButton.Click += StartButtonOnClick;
      stopButton = new ButtonToolItem
      {
        Text = "Stop",
        Enabled = false
      };
      stopButton.Click += StopButtonOnClick;
      clearConsoleButton = new ButtonToolItem
      {
        Text = "Clear consoles",
        Enabled = false
      };
      clearConsoleButton.Click += ClearConsoleButtonOnClick;

      ToolBar = new ToolBar
      {
        Items =
        {
          reloadScriptsButton,
          new SeparatorToolItem(),
          startButton,
          new SeparatorToolItem(),
          stopButton,
          new SeparatorToolItem(),
          clearConsoleButton
        }
      };
    }

    private void InitPanel1()
    {
      scriptsView = new Scripts();
      scriptsView.SelectedItemChanged += ScriptsViewOnSelectedItemChanged;
      scriptsLabel = new Label
      {
        Text = "Scripts"
      };

      panel1 = new StackLayout
      {
        Orientation = Orientation.Vertical,
        Items =
        {
          scriptsLabel,
          scriptsView
        },
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
      };
      panel1.SizeChanged += (sender, args) =>
      {
        if (scriptsView.Height != panel1.Height - scriptsLabel.Height)
          scriptsView.Height = panel1.Height - scriptsLabel.Height;
      };
    }

    private StackLayout InitTasksPanel()
    {
      tasks = new Tasks();
      tasks.SelectedItemChanged += TasksOnSelectedIndexChanged;
      tasksLabel = new Label
      {
        Text = "Tasks"
      };
      var tasksPanel = new StackLayout
      {
        Orientation = Orientation.Vertical,
        Items =
        {
          tasksLabel,
          tasks
        },
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
      };
      tasksPanel.SizeChanged += (sender, args) =>
      {
        if (tasks.Height != tasksPanel.Height - tasksLabel.Height)
          tasks.Height = tasksPanel.Height - tasksLabel.Height;
      };

      return tasksPanel;
    }

    private void InitActionsPanel()
    {
      actionsPanel = new TabControl();
    }

#pragma warning disable CS8618
    public MainForm()
    {
      Title = "Integration tests";
      MinimumSize = new Size(800, 400);
      
      InitToolbar();
      InitPanel1();
      InitActionsPanel();

      panel2 = new Splitter
      {
        Orientation = Orientation.Horizontal,
        Panel1 = InitTasksPanel(),
        Panel2 = actionsPanel,
        BackgroundColor = Colors.White,
        Panel1MinimumSize = 150
      };
      
      Content = new Splitter
      {
        Orientation = Orientation.Horizontal,
        Panel1 = panel1,
        Panel2 = panel2,
        BackgroundColor = Colors.White,
        Panel1MinimumSize = 150
      };

      ReloadScripts();
    }
#pragma warning restore CS8618

    private void ReloadScripts()
    {
      try
      {
        scriptsView.Reload();
      }
      catch (Exception e)
      {
        MessageBox.Show(this, e.Message, "Error");
      }
    }
    private void ScriptsViewOnSelectedItemChanged(object? sender, EventArgs e)
    {
      var item = scriptsView.SelectedItem as ScriptsTreeItem;
      tasks.ShowServices(item?.GetServices());
      if (item is not { Expandable: false })
      {
        stopButton.Enabled = false;
        startButton.Enabled = false;
      }
      else
      {
        if (item.IsStarted)
        {
          stopButton.Enabled = true;
          startButton.Enabled = false;
        }
        else
        {
          stopButton.Enabled = false;
          startButton.Enabled = true;
        }
      }
    }

    private void TasksOnSelectedIndexChanged(object? sender, EventArgs e)
    {
      var item = tasks.SelectedItem as TasksItem;
      actionsPanel.Pages.Clear();
      if (item is not { CanBeStarted: true })
      {
        stopButton.Enabled = false;
        startButton.Enabled = false;
      }
      else
      {
        item.Pages.ForEach(page => actionsPanel.Pages.Add(page));
        
        if (item.IsStarted)
        {
          stopButton.Enabled = true;
          startButton.Enabled = false;
        }
        else
        {
          stopButton.Enabled = false;
          startButton.Enabled = true;
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
      if (tasks.HasFocus)
      {
        StartServices(tasks.SelectedItem as TasksItem);
      }
      else
      {
        StartServices(scriptsView.SelectedItem as ScriptsTreeItem);
      }
    }
    
    private void StopButtonOnClick(object? sender, EventArgs e)
    {
      if (tasks.HasFocus)
      {
        StopServices(tasks.SelectedItem as TasksItem);
      }
      else
      {
        StopServices(scriptsView.SelectedItem as ScriptsTreeItem);
      }
    }
    
    private void ClearConsoleButtonOnClick(object? sender, EventArgs e)
    {
    }
  }
}