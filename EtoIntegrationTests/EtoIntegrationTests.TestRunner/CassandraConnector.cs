using EtoIntegrationTests.Common;
using EtoIntegrationTests.Interfaces;
using Cassandra;

namespace EtoIntegrationTests.TestRunner;

public class CassandraConnector: ICassandraConnector
{
  private readonly CassandraParameters _parameters;
  private ISession? _session;
  public CassandraConnector(CassandraParameters parameters)
  {
    _parameters = parameters;
  }
  
  public void Connect()
  {
    var cluster = Cluster.Builder()
      .AddContactPoint(_parameters.Host)
      .WithPort(_parameters.Port)
      .Build();
    _session = cluster.Connect();
    _session.ChangeKeyspace(_parameters.DbName);
  }

  private void CheckSession()
  {
    if (_session == null)
      throw new InvalidOperationException("null session");
  }
  
  public void TruncateTable(string tableName)
  {
    CheckSession();
    _session!.Execute($"TRUNCATE TABLE {tableName}");
  }

  public ResultSet GetTableData(string tableName)
  {
    CheckSession();
    var rows = _session!.Execute($"SELECT * FROM {tableName}");
    return new ResultSet
    {
      ColumnMapping = rows.Columns.ToDictionary(column => column.Name, column => column.Index),
      Rows = rows.GetRows().Select(row => row.ToList()).ToList()
    };
  }
}