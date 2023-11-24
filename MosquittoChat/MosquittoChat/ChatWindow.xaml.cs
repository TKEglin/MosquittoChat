using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using MQTTnet.Client;

namespace MosquittoChat
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {

        private readonly MqttHandler mqttHandler;
        private string activeTopic = "General"; // The default topic is "General"
        private List<string> subscribedTopics = new() { "General" };

        public ChatWindow(MqttHandler mqttHandler)
        {
            this.mqttHandler = mqttHandler;

            InitializeComponent();

            updateTopicList();
        }

        private void publishButtonClick(object sender, RoutedEventArgs e)
        {
            var msg = msg_textbox.Text;
            mqttHandler.publish(activeTopic, msg);
        }
        private void msg_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                publishButtonClick(sender, e);
            }
        }

        private void mainWindowClosing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttHandler.disconnect();
        }

        private void topicAddTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            var topic = topicAddTextbox.Text;
            if (e.Key == Key.Enter && !subscribedTopics.Contains(topic))
            {
                subscribedTopics.Add(topic);

                updateTopicList();
            }
        }

        private void updateTopicList()
        {
            subscribedTopicsList.Items.Clear();
            foreach (var topic in subscribedTopics)
            {
                subscribedTopicsList.Items.Add(topic);
            }
        }
    }
}
