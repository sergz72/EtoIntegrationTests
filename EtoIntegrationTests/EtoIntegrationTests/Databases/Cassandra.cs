using System.Collections.Generic;
using System.Linq;
using Cassandra;

namespace EtoIntegrationTests.Databases;

public class Cassandra: IDatabase
{
  private string? _hostName, _dbName;
  private int _port;
  private ISession? _session;

  public bool SetParameters(string? parameters)
  {
    var parts = parameters?.Split(' ');
    if (parts is not { Length: 3 } || !int.TryParse(parts[1], out _port))
      return false;
    _hostName = parts[0];
    _dbName = parts[2];
    return true;
  }

  public void Connect()
  {
    var cluster = Cluster.Builder()
      .AddContactPoint(_hostName)
      .WithPort(_port)
      .Build();
    _session = cluster.Connect();
    _session.ChangeKeyspace(_dbName);
  }

  public void Execute(string statement)
  {
    _session?.Execute(statement);
  }

  public void Disconnect()
  {
    _session?.Dispose();
    _session = null;
  }

  public IEnumerable<string> GetTableNames()
  {
    if (_session == null)
      return new List<string>();
    return _session.Execute($"SELECT * FROM system_schema.tables WHERE keyspace_name = '{_dbName}'")
      .Select(row => row.GetValue<string>("table_name"));
  }

  public RowSet GetTableRows(string tableName)
  {
    if (_session == null)
      return new RowSet();
    var rows = _session.Execute($"select * from {_dbName}.{tableName}");
    return new RowSet
    {
      Columns = rows.Columns.Select(column => column.Name).ToList(),
      Rows = rows.GetRows().Select(row => row.ToList()).ToList()
    };
  }
}