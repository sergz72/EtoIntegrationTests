using EtoIntegrationTests.Common;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests.TestRunner;

public class KafkaConnector: IKafkaConnector
{
  private readonly KafkaParameters _parameters;
  public KafkaConnector(KafkaParameters parameters)
  {
    _parameters = parameters;
  }
}