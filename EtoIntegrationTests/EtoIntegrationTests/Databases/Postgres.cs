using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace EtoIntegrationTests.Databases;

public class Postgres: IDatabase
{
  private string? _hostName, _dbName, _dbUser, _dbPassword;
  private int _port;
  private Dictionary<string, string>? dbParameters;
  private NpgsqlConnection? _connection;

  public bool SetParameters(string? parameters)
  {
    var parts = parameters?.Split(' ');
    if (parts == null || parts.Length < 5 || !int.TryParse(parts[1], out _port))
      return false;
    _hostName = parts[0];
    _dbName = parts[2];
    _dbUser = parts[3];
    _dbPassword = parts[4];
    dbParameters = new Dictionary<string, string>();
    for (var n = 5; n < parts.Length; n++)
    {
      var parameter = parts[n].Split(':');
      if (parameter.Length == 2)
      {
        dbParameters[parameter[0]] = parameter[1];
      }
    }

    return true;
  }

  public void Connect()
  {
    var connString = $"Host={_hostName};Username={_dbUser};Password={_dbPassword};Database={_dbName};Port={_port}";

    _connection = new NpgsqlConnection(connString);
    _connection.Open();
    if (dbParameters?.Count > 0)
    {
      foreach (var parameter in dbParameters)
      {
        var reader = new NpgsqlCommand($"SELECT set_config('{parameter.Key}', '{parameter.Value}', true)", _connection)
          .ExecuteReader();
        reader.Close();
      }
    }
  }

  public void Execute(string statement)
  {
    new NpgsqlCommand(statement, _connection).ExecuteNonQuery();
  }

  public void Disconnect()
  {
    _connection?.Close();
  }

  public IEnumerable<string> GetTableNames()
  {
    var tablespaces = new List<string>();
    using (var reader =
           new NpgsqlCommand("SELECT nspname FROM pg_catalog.pg_namespace", _connection).ExecuteReader())
    {
      while (reader.Read())
      {
        var tablespaceName = reader.GetString(0);
        if (tablespaceName != "pg_catalog" && tablespaceName != "information_schema")
          tablespaces.Add(tablespaceName);
      }
    }

    var result = new List<string>();
    foreach (var tablespaceName in tablespaces)
    {
      using (var reader = new NpgsqlCommand(
               $"SELECT table_name FROM information_schema.tables WHERE table_schema='{tablespaceName}' AND table_type='BASE TABLE'",
               _connection).ExecuteReader()) {
        while (reader.Read())
          result.Add(tablespaceName + "." + reader.GetString(0));
      }
    }

    return result;
  }

  public RowSet GetTableRows(string tableName)
  {
    using var reader = new NpgsqlCommand($"SELECT * from {tableName}", _connection).ExecuteReader();
    var result = new RowSet
    {
      Columns = reader.GetColumnSchema().Select(column => column.ColumnName).ToList(),
      Rows = new List<List<object>>()
    };
    while (reader.Read())
      result.Rows.Add(BuildRow(reader));
    return result;
  }

  private List<object> BuildRow(NpgsqlDataReader reader)
  {
    var result = new List<object>();
    for (int column = 0; column < reader.FieldCount; column++)
      result.Add(reader[column]);
    return result;
  }
}