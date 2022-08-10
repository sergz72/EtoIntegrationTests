using System.Collections.Generic;

namespace EtoIntegrationTests.Databases;

public struct RowSet
{
  public List<string> Columns;
  public List<List<object>> Rows;
}

public interface IDatabase
{
  bool SetParameters(string? parameters);
  void Connect();
  void Execute(string statement);
  void Disconnect();
  IEnumerable<string> GetTableNames();
  RowSet GetTableRows(string tableName);
}