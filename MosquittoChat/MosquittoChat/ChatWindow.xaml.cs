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

        // The topic prefixes are used to group communication topics and avoid interference with other application using the network
        private const string GeneralTopicPrefix = "MosquittoChat";
        private const string MessageTopicPrefix = "Messaging";
        private const string ConfigTopicPrefix = "Config";
        // The active topic is the topic that is currently visible in the message view of the UI
        private string activeTopic;

        public ChatWindow(MqttHandler mqttHandler, string username)
        {
            this.mqttHandler = mqttHandler;
            this.username = username;

            this.mqttHandler.MessageReceived += e =>
            {
                // When message received, message is added to the list of the corresponding topic:
                var topic = RetrieveTopicString(e.Topic);
                subscribedTopics[topic].Add(e.Message);
                // UI is only updated if the message belongs to the active topic
                if (activeTopic == topic)
                    AddMessageToMessageView(e.Message);
            };

            this.activeTopic = DefaultTopic;

            // UI
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddTopicAndSetActive(DefaultTopic);
            Subscribe(DefaultTopic);
        }

        private void PublishButtonClick(object sender, RoutedEventArgs e)
        {
            var msg = GenerateMessageText(msg_textbox.Text);
            mqttHandler.publish(GenerateMessageTopic(activeTopic), msg);
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

        /// <summary>
        /// Subscribes to the given topic using the mqtt handler.
        /// </summary>
        private void Subscribe(string topic)
        {
            this.mqttHandler.subscribe(GenerateMessageTopic(topic));
        }

        /// <summary>
        /// Generates a full topic string using the general and message prefixes
        /// </summary>
        public string GenerateMessageTopic(string topic)
        {
            return $"{GeneralTopicPrefix}/{MessageTopicPrefix}/{topic}";
        }

        /// <summary>
        /// Retrieves the specific topic of a full topic string
        /// </summary>
        private string RetrieveTopicString(string topic)
        {
            var topicComponents = topic.Split("/");
            return topicComponents.Last();
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
                AddTopicAndSetActive(topic);
                Subscribe(topic);
            }
        }

        /// <summary>
        /// Adds a topic to the UI listbox, subscribes using the handler and sets the topic active.
        /// Should only be used once on the creation of the topic.
        /// </summary>
        private void AddTopicAndSetActive(string topic)
        {
            //Subscribing
            subscribedTopics[topic] = new List<string>();

            //Adding to UI
            this.Dispatcher.Invoke(() =>
            {
                subscribedTopicsListBox.Items.Add(topic);
                subscribedTopicsListBox.SelectedIndex = subscribedTopicsListBox.Items.Count - 1;
            });

            //Setting active
            activeTopic = topic;
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

        private void subscribedTopicsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] != null && (string)e.AddedItems[0]! != null)
            {
                activeTopic = (string)e.AddedItems[0]!;
                ReloadMessageView();
            }
        }
    }
}
