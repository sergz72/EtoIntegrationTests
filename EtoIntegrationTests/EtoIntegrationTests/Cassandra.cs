using System;
using System.Linq;
using System.Net;
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
      var tableNames = session
        .Execute($"SELECT * FROM system_schema.tables WHERE keyspace_name = '{_dbName}'")
        .Select(row => row.GetValue<string>("table_name"));
      _tabs.Pages.Clear();
      foreach (var tableName in tableNames)
      {
        
      }
    }
    catch (Exception exception)
    {
      Console.WriteLine(exception);
      throw;
    }
  }
}
