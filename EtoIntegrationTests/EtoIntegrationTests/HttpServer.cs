using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using EtoIntegrationTests.Common;

namespace EtoIntegrationTests;

public class HttpServer
{
  private readonly HttpListener _listener;
  private readonly Tests _tests;
  
  public HttpServer(Tests tests)
  {
    _tests = tests;
    _listener = new HttpListener();
    _listener.Prefixes.Add("http://127.0.0.1:9999/");
    _listener.Start();
    Task.Run(ServerLoop);
  }

  private void ServerLoop()
  {
    while (true)
    {
      HttpListenerContext context = _listener.GetContext();
      HttpListenerRequest request = context.Request;
      var parameters = _tests.GetParameters();
      var services = _tests.GetServices();
      if (parameters != null && services != null)
      {
        if (request.Url != null)
        {
          var p = request.Url.PathAndQuery;
          if (p.StartsWith("/console/"))
          {
            var serviceName = p.Substring(9);
            if (services.ContainsKey(serviceName))
            {
              if (request.HttpMethod == HttpMethod.Get.Method)
              {
                SendResponse(context, 200, string.Join("\n", services[serviceName].GetLogs()));
                continue;
              }
              if (request.HttpMethod == HttpMethod.Delete.Method)
              {
                services[serviceName].ClearLogs();
                SendResponse(context, 204, null);
                continue;
              }
            }
          }
          else if (p == "/parameters" && request.HttpMethod == HttpMethod.Get.Method)
          {
            var commonParameters = new CommonParameters
            {
              TestParameters = parameters,
              Services = services.Select(service => service.Key).ToList(),
              ServiceUrls = services.Where(service => service.Value.GetUrlForTests().Length > 0)
                .ToDictionary(service => service.Key, service => service.Value.GetUrlForTests())
            };
            string jsonString = JsonSerializer.Serialize(commonParameters);
            SendResponse(context, 200, jsonString);
            continue;
          }
        }
      }

      SendResponse(context, 400, "Bad request");
    }
  }

  private void SendResponse(HttpListenerContext context, int statusCode, string? content)
  {
    HttpListenerResponse response = context.Response;
    response.StatusCode = statusCode;
    if (content != null)
    {
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
      response.ContentLength64 = buffer.Length;
      System.IO.Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
    }
  }
}