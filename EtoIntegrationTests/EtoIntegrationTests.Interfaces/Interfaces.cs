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
}