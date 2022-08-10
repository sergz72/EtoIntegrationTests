using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Eto.Forms;
using EtoIntegrationTests.Databases;

namespace EtoIntegrationTests;

class CassandraPageBuilder : IPageBuilder
{
  public TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger)
  {
    return new TabPage{ Content = new DatabaseClient(new Databases.Cassandra(), parameters), Text = pageName };
  }
}

class PostgresPageBuilder : IPageBuilder
{
  public TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger)
  {
    return new TabPage{ Content = new DatabaseClient(new Postgres(), parameters), Text = pageName };
  }
}

class DatabaseClient : StackLayout
{
  private readonly TabControl _tabs;
  private readonly ListBox _messages;
  private readonly IDatabase _database;
  
  public DatabaseClient(IDatabase database, string? parameters)
  {
    _tabs = new TabControl();
    _messages = new ListBox();
    Orientation = Orientation.Vertical;
    HorizontalContentAlignment = HorizontalAlignment.Stretch;
    
    var refreshButton = new Button
    {
      Text = "Refresh"
    };
    refreshButton.Click += RefreshButtonOnClick;
    var runCqlButton = new Button
    {
      Text = "Run CQL"
    };
    runCqlButton.Click += RunCQLButtonOnClick;
    Items.Add(new StackLayoutItem
    {
      Control = new StackLayout
      {
        Orientation = Orientation.Horizontal,
        Items =
        {
          new StackLayoutItem
          {
            Control = refreshButton,
            Expand = true
          },
          new StackLayoutItem
          {
          Control = runCqlButton,
          Expand = true
        }
        }
      }
    });
    Items.Add(new StackLayoutItem
    {
      Control = _tabs,
      Expand = true
    });
    Items.Add(new StackLayoutItem
    {
      Control = _messages,
      Expand = true
    });

    _database = database;
    if (!_database.SetParameters(parameters))
    {
      _messages.Items.Add("Incorrect parameters");
      refreshButton.Enabled = false;
    }
  }

  private void RunCQLButtonOnClick(object? sender, EventArgs e)
  {
    var dialog = new OpenFileDialog();
    dialog.MultiSelect = true;
    dialog.Directory = new Uri(Directory.GetCurrentDirectory());

    try
    {
      if (dialog.ShowDialog(this) == DialogResult.Ok)
      {
        _messages.Items.Clear();
        _database.Connect();
        _messages.Items.Add("Connected to database...");
        foreach (var fileName in dialog.Filenames)
        {
          _messages.Items.Add($"Executing {fileName}");
          var sb = new StringBuilder();
          foreach (var line in File.ReadLines(fileName))
          {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("--"))
              continue;
            if (trimmed.Contains(';'))
            {
              sb.Append(trimmed);
              var statement = sb.ToString();
              _messages.Items.Add(statement);
              _database.Execute(statement);
              sb.Clear();
            }
            else
              sb.Append(trimmed);
          }
        }
      }
    }
    catch (Exception exception)
    {
      _messages.Items.Add(exception.Message);
    }
    finally
    {
      _database.Disconnect();
    }
  }

  private void RefreshButtonOnClick(object? sender, EventArgs e)
  {
    _messages.Items.Clear();
    try
    {
      _database.Connect();
      _messages.Items.Add("Connected to database...");
      var tableNames = _database.GetTableNames();
      _tabs.Pages.Clear();
      foreach (var tableName in tableNames)
      {
        _tabs.Pages.Add(BuildTabPage(tableName));
      }
    }
    catch (Exception exception)
    {
      _messages.Items.Add(exception.Message);
    }
    finally
    {
      _database.Disconnect();
    }
  }

  private TabPage BuildTabPage(string tableName)
  {
    return new TabPage { Text = tableName, Content = BuildPageContent(tableName) };
  }

  private Control BuildPageContent(string tableName)
  {
    var content = new TreeGridView();
    var rows = _database.GetTableRows(tableName);

    int idx = 0;
    foreach (var column in rows.Columns)
    {
      content.Columns.Add(new GridColumn
      {
        HeaderText = column,
        DataCell = new TextBoxCell
        {
          Binding = new DatabaseColumnBinding(idx)
        }
      });
      idx++;
    }

    content.DataStore = new DatabaseDataStore(rows);

    return content;
  }
}

internal class DatabaseColumnBinding : IIndirectBinding<string>
{
  private readonly int _idx;
  public DatabaseColumnBinding(int idx)
  {
    _idx = idx;
  }

  public void Unbind()
  {
    throw new NotImplementedException();
  }

  public void Update(BindingUpdateMode mode = BindingUpdateMode.Source)
  {
  }

  public string GetValue(object dataItem)
  {
    return (dataItem as DatabaseTreeGridItem)![_idx];
  }

  public void SetValue(object dataItem, string value)
  {
    throw new NotImplementedException();
  }
}

internal class DatabaseDataStore : ITreeGridStore<DatabaseTreeGridItem>
{
  public DatabaseDataStore(RowSet rows)
  {
    _rows = rows.Rows.Select(row => new DatabaseTreeGridItem(row)).ToList();
  }

  public int Count => _rows.Count;

  private readonly List<DatabaseTreeGridItem> _rows;

  public DatabaseTreeGridItem this[int index] => _rows[index];
}

internal class DatabaseTreeGridItem : ITreeGridItem
{
  public bool Expanded { get; set; }
  public bool Expandable => false;
  public ITreeGridItem? Parent { get; set; }
  
  public string this[int index] => _row[index].ToString() ?? "null";

  private readonly List<object> _row;
  
  public DatabaseTreeGridItem(List<object> row)
  {
    _row = row;
  }
}
