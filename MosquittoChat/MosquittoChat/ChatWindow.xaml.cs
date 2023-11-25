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
using System.Windows.Interop;

namespace MosquittoChat
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly MqttHandler mqttHandler;
        private readonly string username;

        // Topic threads are stored in a dictionary that ties topic names to lists of message strings
        //      Messages strings include the username of the sender
        private const string DefaultTopic = "General";
        private Dictionary<string, List<string>> subscribedTopics = new() { { DefaultTopic, new List<string>() } };

        // The active topic is the topic that is currently visible in the message view of the UI
        private string activeTopic;

        public ChatWindow(MqttHandler mqttHandler, string username)
        {
            this.mqttHandler = mqttHandler;
            this.username = username;

            this.mqttHandler.MessageReceived += e =>
            {
                // When message received, message is added to the list of the corresponding topic:
                subscribedTopics[e.Topic].Add(e.Message);
                // UI is only updated if the message belongs to the active topic
                if (activeTopic == e.Topic) 
                    AddMessageToMessageView(e.Message);
            };

            this.activeTopic = subscribedTopics.Keys.First();
            this.mqttHandler.subscribe(activeTopic);

            // UI
            InitializeComponent();
            UpdateTopicList();
            subscribedTopicsListBox.SelectedItem = subscribedTopicsListBox.Items[0];
        }

        private void PublishButtonClick(object sender, RoutedEventArgs e)
        {
            var msg = GenerateMessageText(msg_textbox.Text);
            mqttHandler.publish(activeTopic, msg);
            msg_textbox.Text = "";
        }
        private void msg_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PublishButtonClick(sender, e);
            }
        }

        public string GenerateMessageText(string content)
        {
            var time = DateTime.Now;
            string msg = $"[{time.ToString("T")}] {username}: {content}";
            return msg;
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttHandler.disconnect();
        }

        private void TopicAddTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            var topic = topicAddTextbox.Text;
            if (e.Key == Key.Enter && !subscribedTopics.ContainsKey(topic) && topic.Length > 0)
            {
                topicAddTextbox.Text = string.Empty;

                subscribedTopics[topic] = new List<string>();
                this.mqttHandler.subscribe(topic);
                UpdateTopicList();
            }
        }

        private void UpdateTopicList()
        {
            // Note: the dispatcher is needed to avoid threading issues
            this.Dispatcher.Invoke(() =>
            {
                subscribedTopicsListBox.Items.Clear();
                foreach (var topic in subscribedTopics.Keys)
                {
                    subscribedTopicsListBox.Items.Add(topic);
                    //Setting selected to last inserted if it is the currently active topic
                    if (topic == activeTopic)
                        subscribedTopicsListBox.SelectedIndex = subscribedTopicsListBox.Items.Count - 1;
                }
            });
        }

        private void AddMessageToMessageView(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                messageViewListBox.Items.Add(message);
            });
        }

        private void ReloadMessageView()
        {
            this.Dispatcher.Invoke(() =>
            {
                messageViewListBox.Items.Clear();
                foreach (var msg in subscribedTopics[activeTopic])
                {
                    messageViewListBox.Items.Add(msg);
                }
            });
        }

        private void subscribedTopicsListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            activeTopic = subscribedTopicsListBox.SelectedItem.ToString() ?? DefaultTopic;
            ReloadMessageView();
        }
    }
}
