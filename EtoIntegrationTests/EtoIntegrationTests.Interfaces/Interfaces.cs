using System.Collections.Generic;

namespace EtoIntegrationTests.Interfaces
{
  public interface IService
  {
    List<string> GetLogs();
    void ClearLogs();
    string GetUrlForTests();
  }

  public struct Connectors
  {
    public IKafkaConnector KafkaConnector;
    public ICassandraConnector CassandraConnector;
  }

  public interface IKafkaConnector
  {
  }

  public interface ICassandraConnector
  {
    void Connect();
    void TruncateTable(string tableName);
    ResultSet GetTableData(string tableName);
  }

  public struct ResultSet
  {
    public Dictionary<string, int> ColumnMapping;
    public List<List<object>> Rows;
  }

  public interface ITestLogger
  {
    void Log(string line);
  }
  
  public struct TestResult
  {
    public static readonly TestResult Successful = new TestResult{
      Success = true
    };
    public bool Success;
    public string ErrorMessage;

    public static TestResult Failure(string format, params object[] parameters)
    {
      return new TestResult
      {
        Success = false,
        ErrorMessage = string.Format(format, parameters)
      };
    }
  }
  
  public delegate TestResult TestDelegate();
  
  public interface ITests
  {
    Dictionary<string, TestDelegate> Init(Connectors connectors, Dictionary<string, IService> services, ITestLogger logger);
  }
}