using System.Net;
using System.Runtime.Loader;
using System.Text.Json;
using EtoIntegrationTests.Common;
using EtoIntegrationTests.Interfaces;

namespace EtoIntegrationTests.TestRunner;

class TestRunner : ITestLogger
{
  private readonly string _folder;
  private readonly ITestParameters _parameters;
  private readonly Dictionary<string, IService> _services;
  
  static void Main(string[] args)
  {
    try
    {
      switch (args.Length)
      {
        case 0:
        case 1:
          Usage();
          break;
        case 2:
          switch (args[1])
          {
            case "list":
              new TestRunner(args[0]).ShowTestList();
              break;
            case "run":
              new TestRunner(args[0]).RunAllTests();
              break;
            default:
              Usage();
              break;
          }
          break;
        default:
          if (args[1] == "run")
          {
            new TestRunner(args[0]).RunTests(args[2..]);
          }
          else
            Usage();
          break;
      }
    }
    catch (Exception e)
    {
      Console.Error.WriteLine("##fatal {0}", e);
      throw;
    }
  }

  private static void Usage()
  {
    Console.Error.WriteLine("Usage: TestRunner folder_name list|run [test_names]");
  }

  private TestRunner(string folder)
  {
    _folder = folder;
    var parameters = GetCommonParameters();
    _parameters = parameters.TestParameters;
    _services = parameters.Services.ToDictionary(service => service,
      service => new Service(service, parameters) as IService);
  }
  
  private void RunTests(string[] testNames)
  {
    TestsHandler(tests =>
    {
      foreach (var test in tests
                 .Where(test => Array.Exists(testNames, e => e == test.Key)))
        RunTest(test.Key, test.Value);
    });
  }

  private void RunAllTests()
  {
    TestsHandler(tests =>
    {
      foreach (var test in tests)
        RunTest(test.Key, test.Value);
    });
  }

  private void ShowTestList()
  {
    TestsHandler(tests =>
    {
      foreach (var test in tests)
        Console.WriteLine(test.Key);
    });
  }

  internal static string GetData(string endpoint)
  {
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
      RequestUri = new Uri($"http://127.0.0.1:9999/{endpoint}"),
      Method = HttpMethod.Get
    };
    var response = client.Send(request);
    if (response.StatusCode != HttpStatusCode.OK)
      throw new InvalidDataException($"Wrong response code to get {endpoint} request");
    var stream = response.Content.ReadAsStream();
    return new StreamReader(stream).ReadToEnd();
  }

  internal static void DeleteData(string endpoint)
  {
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
      RequestUri = new Uri($"http://127.0.0.1:9999/{endpoint}"),
      Method = HttpMethod.Delete
    };
    var response = client.Send(request);
    if (response.StatusCode != HttpStatusCode.NoContent)
      throw new InvalidDataException($"Wrong response code to delete {endpoint} request");
  }

  private CommonParameters GetCommonParameters()
  {
    return JsonSerializer.Deserialize<CommonParameters>(GetData("parameters"));
  }

  private void TestsHandler(Action<Dictionary<string, TestDelegate>> handler)
  {
    var testsFile = Directory.GetFiles(_folder, "*.dll").FirstOrDefault();
    if (testsFile == null)
      return;
    var a = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(testsFile));
    foreach (var type in a.GetTypes())
    {
      if (type.GetInterfaces().Contains(typeof(ITests)))
      {
        var t = Activator.CreateInstance(type) as ITests;
        var tests = t?.Init(_parameters, _services, this);
        if (tests != null)
          handler(tests);
      }
    }
  }

  public void Log(string line)
  {
    Console.WriteLine(line);
  }
  
  private void RunTest(string name, TestDelegate d)
  {
    Console.WriteLine($"##started {name}");
    try
    {
      var result = d.Invoke();
      Console.WriteLine(result.Success ? "##success" : $"##failure {result.ErrorMessage}");
    }
    catch (Exception e)
    {
      Console.WriteLine($"##failure {e.Message}");
    }
  }
}

class Service : IService
{
  private readonly string _name;
  private readonly string _url;
  internal Service(string name, CommonParameters parameters)
  {
    _name = name;
    _url = parameters.ServiceUrls.ContainsKey(name) ? parameters.ServiceUrls[name] : "";
  }

  public List<string> GetLogs()
  {
    var content = TestRunner.GetData($"console/{_name}");
    return content.Split('\n').ToList();
  }

  public void ClearLogs()
  {
    TestRunner.DeleteData($"console/{_name}");
  }

  public string GetUrlForTests()
  {
    return _url;
  }
}