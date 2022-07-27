using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EtoIntegrationTests.Interfaces;

namespace ExampleTests;

public class ExampleTest: ITests
{
  private const string ServiceName = "server";

  private Connectors _connectors;
  private Dictionary<string, IService>? _services;
  private ITestLogger? _logger;
    
  public Dictionary<string, TestDelegate> Init(Connectors connectors, Dictionary<string, IService> services, ITestLogger logger)
  {
    _connectors = connectors;
    _services = services;
    _logger = logger;
    return new Dictionary<string, TestDelegate>
    {
      {"MessageTest", MessageTest}
    };
  }

  private TestResult MessageTest()
  {
    _logger!.Log("Starting test...");
    var client = new HttpClient();
    var message = new HttpRequestMessage
    {
      Method = HttpMethod.Post,
      RequestUri = new Uri(_services![ServiceName].GetUrlForTests() + "/testAPI"),
      Content = new StringContent("{\"name\":\"value\"}", Encoding.Default, "application/json")
    };
    _services["testService"].ClearLogs();

    _connectors.CassandraConnector.Connect();
    _logger.Log("Connected to database...");
    _connectors.CassandraConnector.TruncateTable("messages");

    
    var response = client.Send(message);
    if (response.StatusCode != HttpStatusCode.OK)
    {
      return TestResult.Failure("wrong status code: {0}", response.StatusCode);
    }
    Thread.Sleep(1000);// 1 second
    var testLogs = _services["testService"].GetLogs();
    if (testLogs.Count != 2)
    {
      return TestResult.Failure("wrong logs line count: {0}", testLogs.Count);
    }
    if (!testLogs[0].Contains("is valid"))
    {
      return TestResult.Failure("wrong logs line: {0}", testLogs[0]);
    }

    var rows = _connectors.CassandraConnector.GetTableData("messages");
    if (rows.Rows.Count != 1)
    {
      return TestResult.Failure("wrong messages table rows count: {0}", rows.Rows.Count);
    }

    return TestResult.Successful;
  }
}