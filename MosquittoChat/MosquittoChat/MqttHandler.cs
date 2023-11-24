using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public MqttHandler()
        {
            var mqttFactory = new MqttFactory();
            this.mqttClient = mqttFactory.CreateMqttClient();
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

            this.mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

            MessageBox.Show("Published Message to Topic");
        }

        public void subscribe(string topic)
        {
            this.mqttClient.SubscribeAsync(topic);
        }
    }
}
