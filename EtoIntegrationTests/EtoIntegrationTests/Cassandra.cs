using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra;
using Eto.Forms;

namespace EtoIntegrationTests;

class CassandraPageBuilder : IPageBuilder
{
  public TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger)
  {
    return new TabPage{ Content = new CassandraClient(parameters), Text = pageName };
  }
}

class CassandraClient : StackLayout
{
  private readonly TabControl _tabs;
  private readonly ListBox _messages;
  private readonly string _hostName, _dbName;
  private readonly int _port;
  
  public CassandraClient(string? parameters)
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
    Items.Add(new StackLayoutItem
    {
      Control = refreshButton
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
    
    var parts = parameters?.Split(' ');
    if (parts is not { Length: 3 } || !int.TryParse(parts[1], out _port))
    {
      _messages.Items.Add("Incorrect parameters");
      refreshButton.Enabled = false;
      _hostName = "";
      _dbName = "";
      return;
    }

    _hostName = parts[0];
    _dbName = parts[2];
  }

  private void RefreshButtonOnClick(object? sender, EventArgs e)
  {
    try
    {
      var cluster = Cluster.Builder()
        .AddContactPoint(_hostName)
        .WithPort(_port)
        .Build();
      using var session = cluster.Connect();
      _messages.Items.Add("Connected to database...");
      var tableNames = session
        .Execute($"SELECT * FROM system_schema.tables WHERE keyspace_name = '{_dbName}'")
        .Select(row => row.GetValue<string>("table_name"));
      _tabs.Pages.Clear();
      _messages.Items.Clear();
      foreach (var tableName in tableNames)
      {
        _tabs.Pages.Add(BuildTabPage(session, tableName));
      }
    }
    catch (Exception exception)
    {
      _messages.Items.Add(exception.Message);
    }
  }

  private TabPage BuildTabPage(ISession session, string tableName)
  {
    return new TabPage { Text = tableName, Content = BuildPageContent(session, tableName) };
  }

  private Control BuildPageContent(ISession session, string tableName)
  {
    var content = new TreeGridView();
    var rows = session.Execute($"select * from {_dbName}.{tableName}");

    int idx = 0;
    foreach (var column in rows.Columns)
    {
      content.Columns.Add(new GridColumn
      {
        HeaderText = column.Name,
        DataCell = new TextBoxCell
        {
          Binding = new CassandraColumnBinding(idx)
        }
      });
      idx++;
    }

    content.DataStore = new CassandraDataStore(rows);

    return content;
  }
}

internal class CassandraColumnBinding : IIndirectBinding<string>
{
  private readonly int _idx;
  public CassandraColumnBinding(int idx)
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
    return (dataItem as CassandraTreeGridItem)![_idx];
  }

  public void SetValue(object dataItem, string value)
  {
    throw new NotImplementedException();
  }
}

internal class CassandraDataStore : ITreeGridStore<CassandraTreeGridItem>
{
  public CassandraDataStore(RowSet rows)
  {
    _rows = rows.Select(row => new CassandraTreeGridItem(row)).ToList();
  }

  public int Count => _rows.Count;

  private readonly List<CassandraTreeGridItem> _rows;

  public CassandraTreeGridItem this[int index] => _rows[index];
}

internal class CassandraTreeGridItem : ITreeGridItem
{
  public bool Expanded { get; set; }
  public bool Expandable => false;
  public ITreeGridItem? Parent { get; set; }
  
  public string this[int index] => _row[index].ToString() ?? "null";

  private readonly Row _row;
  
  public CassandraTreeGridItem(Row row)
  {
    _row = row;
  }
}
