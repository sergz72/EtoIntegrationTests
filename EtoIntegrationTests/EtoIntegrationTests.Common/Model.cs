using EtoIntegrationTests.Interfaces;
using YamlDotNet.Serialization;

namespace EtoIntegrationTests.Common;

public class Parameters: ITestParameters
{
  public KafkaParameters Kafka { get; set; }
  public CassandraParameters Cassandra { get; set; }

  public Parameters()
  {
    Kafka = new KafkaParameters();
    Cassandra = new CassandraParameters();
  }

  public IKafkaParameters GetKafkaParameters()
  {
    return Kafka;
  }

  public ICassandraParameters GetCassandraParameters()
  {
    return Cassandra;
  }
}

public class KafkaParameters: IKafkaParameters
{
  public string Host { get; set; }
  
  public Dictionary<string, KafkaTopicParameters> Topics { get; set; }

  public KafkaParameters()
  {
    Host = "";
    Topics = new Dictionary<string, KafkaTopicParameters>();
  }

  public string GetHost()
  {
    return Host;
  }

  public Dictionary<string, IKafkaTopicParameters> GetTopics()
  {
    return Topics.ToDictionary(topic => topic.Key, topic => topic.Value as IKafkaTopicParameters);
  }
}

public class KafkaTopicParameters: IKafkaTopicParameters
{
  public string Name { get; set; }
  public string Group { get; set; }

  public KafkaTopicParameters()
  {
    Name = "";
    Group = "";
  }

  public string GetName()
  {
    return Name;
  }

  public string GetGroup()
  {
    return Group;
  }
}

public class CassandraParameters: ICassandraParameters
{
  public string Host { get; set; }
  public int Port { get; set; }
  [YamlMember(Alias = "db_name", ApplyNamingConventions = false)]
  public string DbName { get; set; }

  public CassandraParameters()
  {
    Host = "";
    DbName = "";
  }

  public string GetHost()
  {
    return Host;
  }

  public int GetPort()
  {
    return Port;
  }

  public string GetDbName()
  {
    return DbName;
  }
}

public struct CommonParameters
{
  public Parameters TestParameters { get; set; }
  public List<string> Services { get; set; }
  public Dictionary<string, string> ServiceUrls { get; set; }
}
