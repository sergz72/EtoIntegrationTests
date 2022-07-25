using System.Collections.Generic;

namespace EtoIntegrationTests.Interfaces
{
  public interface IService
  {
    List<string> GetLogs();
    void ClearLogs();
    string GetUrlForTests();
  }

  public interface ITestParameters
  {
    IKafkaParameters GetKafkaParameters();
    ICassandraParameters GetCassandraParameters();
  }

  public interface IKafkaParameters
  {
    string GetHost();
    Dictionary<string, IKafkaTopicParameters> GetTopics();
  }

  public interface IKafkaTopicParameters
  {
    string GetName();
    string GetGroup();
  }

  public interface ICassandraParameters
  {
    string GetHost();
    int GetPort();
    string GetDbName();
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
    Dictionary<string, TestDelegate> Init(ITestParameters parameters, Dictionary<string, IService> services, ITestLogger logger);
  }
}