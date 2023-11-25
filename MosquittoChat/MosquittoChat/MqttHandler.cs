using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MosquittoChat
{
    public class MqttHandler
    {
        private IMqttClient mqttClient;

        public class MessageEventArgs
        {
            public MessageEventArgs(string msg, string topic)
            {
                Message = msg;
                Topic = topic;
            }
            public string Message { get; set; }
            public string Topic { get; set; }
        }

        public delegate void MessageEventHandler(MessageEventArgs e);
        public event MessageEventHandler? MessageReceived;


        public MqttHandler()
        {
            var mqttFactory = new MqttFactory();
            this.mqttClient = mqttFactory.CreateMqttClient();

            // When a message is received, the payload and topic is passed on to an external event handler
            this.mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var Message = e.ApplicationMessage;
                var Payload = Encoding.Default.GetString(Message.PayloadSegment);

                this.MessageReceived?.Invoke(new MessageEventArgs(Payload, Message.Topic));

                return Task.CompletedTask;
            };

        }

        public void connect(string IP, int port)
        {
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(IP, port).Build();
            var response = this.mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            Debug.WriteLine(response.ToString());
        }

        public void disconnect() 
        {
            mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
        }

        public void publish(string topic, string msg)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .Build();

            Debug.WriteLine($"Publishing to {topic} the message: {msg}");
            this.mqttClient.PublishAsync(mqttMessage, CancellationToken.None);
        }

        public void subscribe(string topic)
        {
            this.mqttClient.SubscribeAsync(topic);
        }
    }
}
