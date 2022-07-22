using System;
using System.Net;
using System.Threading.Tasks;
using Confluent.Kafka;
using Eto.Forms;

namespace EtoIntegrationTests;

class KafkaPageBuilder : IPageBuilder
{
  public TabPage BuildPage(string pageName, string? parameters, ConsoleLogger logger)
  {
    return new TabPage{ Content = new KafkaClient(parameters), Text = pageName };
  }
}

class KafkaClient : StackLayout
{
  private readonly string _hostName, _topicName;
  private readonly TextArea _text;
  private readonly ListBox _messages;
  
  public KafkaClient(string? parameters)
  {
    _text = new TextArea();
    _messages = new ListBox();
    var sendButton = new Button
    {
      Text = "Send"
    };
    sendButton.Click += SendButtonOnClick;
    Items.Add(new StackLayoutItem
    {
      Control = _messages,
      Expand = true
    });
    Items.Add(new StackLayoutItem
    {
      Control = _text,
      Expand = true
    });
    Items.Add(new StackLayoutItem
    {
      Control = sendButton
    });
    Orientation = Orientation.Vertical;
    HorizontalContentAlignment = HorizontalAlignment.Stretch;
    
    var parts = parameters?.Split(' ');
    if (parts is not { Length: 3 })
    {
      _text.Text = "Incorrect parameters";
      _text.ReadOnly = true;
      sendButton.Enabled = false;
      _hostName = "";
      _topicName = "";
      return;
    }

    _hostName = parts[0];
    _topicName = parts[1];
    StartConsumer(parts[2]);
  }

  private void StartConsumer(string groupId)
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _hostName,
      GroupId = groupId,
      AutoOffsetReset = AutoOffsetReset.Earliest
    };
    
    Task.Run(() =>
    {
      while (true)
      {
        try
        {
          using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
          consumer.Subscribe(_topicName);

          while (true)
          {
            var consumeResult = consumer.Consume();
            Application.Instance.Invoke(() => _messages.Items.Add(consumeResult.Message.Value));
          }
        }
        catch (Exception)
        {
          //Application.Instance.Invoke(() => Items.Add(e.Message));
        }
      }
    });
  }

  private void SendButtonOnClick(object? sender, EventArgs e)
  {
    var config = new ProducerConfig
    {
      BootstrapServers = _hostName,
      ClientId = Dns.GetHostName()
    };
    try
    {
      using var producer = new ProducerBuilder<Null, string>(config).Build();
      producer.Produce(_topicName, new Message<Null, string> { Value=_text.Text });
      producer.Flush();
    }
    catch (Exception exception)
    {
      MessageBox.Show(this, exception.Message, "Error");
    }
  }
}
