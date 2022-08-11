using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace EtoIntegrationTests.Databases;

public class Postgres: IDatabase
{
  private string? _hostName, _dbName, _dbUser, _dbPassword;
  private int _port;
  private NpgsqlConnection? _connection;

  public bool SetParameters(string? parameters)
  {
    var parts = parameters?.Split(' ');
    if (parts is not { Length: 5 } || !int.TryParse(parts[1], out _port))
      return false;
    _hostName = parts[0];
    _dbName = parts[2];
    _dbUser = parts[3];
    _dbPassword = parts[4];
    return true;
  }

  public void Connect()
  {
    var connString = $"Host={_hostName};Username={_dbUser};Password={_dbPassword};Database={_dbName};Port={_port}";

    _connection = new NpgsqlConnection(connString);
    _connection.Open();
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
    using var reader = 
      new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE'", _connection)
      .ExecuteReader();
    var result = new List<string>();
    while (reader.Read())
      result.Add(reader.GetString(0));
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