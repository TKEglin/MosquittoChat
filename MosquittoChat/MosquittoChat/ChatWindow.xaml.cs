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
        }

        private void publishButtonClick(object sender, RoutedEventArgs e)
        {
            mqttHandler.publish(activeTopic, msg_textbox.Text);
        }

        private void mainWindowClosing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttHandler.disconnect();
        }
    }
}
