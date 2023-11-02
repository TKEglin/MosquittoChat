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
        public readonly MqttHandler mqttHandler;

        public ChatWindow(MqttHandler mqttHandler)
        {
            this.mqttHandler = mqttHandler;



            InitializeComponent();
        }

        private void publishButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttHandler.publish(topic_textbox.Text, msg_textbox.Text);
        }

        private void mainWindowClosing(object sender, CancelEventArgs e)
        {
            mqttHandler.disconnect();
        }
    }
}
