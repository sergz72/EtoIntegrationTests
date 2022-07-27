using YamlDotNet.Serialization;

namespace EtoIntegrationTests.Common;

public class Parameters
{
  public KafkaParameters Kafka { get; set; }
  public CassandraParameters Cassandra { get; set; }

  public Parameters()
  {
    Kafka = new KafkaParameters();
    Cassandra = new CassandraParameters();
  }
}

public class KafkaParameters
{
  public string Host { get; set; }
  
  public Dictionary<string, KafkaTopicParameters> Topics { get; set; }

  public KafkaParameters()
  {
    Host = "";
    Topics = new Dictionary<string, KafkaTopicParameters>();
  }
}

public class KafkaTopicParameters
{
  public string Name { get; set; }
  public string Group { get; set; }

  public KafkaTopicParameters()
  {
    Name = "";
    Group = "";
  }
}

public class CassandraParameters
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
}

public struct CommonParameters
{
  public Parameters TestParameters { get; set; }
  public List<string> Services { get; set; }
  public Dictionary<string, string> ServiceUrls { get; set; }
}
