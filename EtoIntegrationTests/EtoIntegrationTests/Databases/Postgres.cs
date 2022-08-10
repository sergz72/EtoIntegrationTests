using System.Collections.Generic;

namespace EtoIntegrationTests.Databases;

public class Postgres: IDatabase
{
  public bool SetParameters(string? parameters)
  {
    return true;
  }

  public void Connect()
  {
    throw new System.NotImplementedException();
  }

  public void Execute(string statement)
  {
    throw new System.NotImplementedException();
  }

  public void Disconnect()
  {
    throw new System.NotImplementedException();
  }

  public IEnumerable<string> GetTableNames()
  {
    throw new System.NotImplementedException();
  }

  public RowSet GetTableRows(string tableName)
  {
    throw new System.NotImplementedException();
  }
}