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

        // Topic threads are stored in a dictionary that ties topic names to TopicRooms
        //      Messages strings include the username of the sender
        private Dictionary<string, TopicRoom> connectedRooms = new() 
        { 
            { TopicTypes.DefaultMessagingTopic, new TopicRoom(TopicTypes.DefaultMessagingTopic) } 
        };

        // The active topic is the topic that is currently visible in the message view of the UI
        private string activeTopic;

        public ChatWindow(MqttHandler mqttHandler, string username)
        {
            this.mqttHandler = mqttHandler;
            this.username = username;

            this.mqttHandler.MessageReceived += MessageReceivedHandler;

            this.activeTopic = TopicTypes.DefaultMessagingTopic;

            // UI
            InitializeComponent();
        }

        private void MessageReceivedHandler(MqttHandler.MessageEventArgs e)
        {
            var topicComponents = e.Topic.Split("/");
            var specificTopic = topicComponents.Last();

            if (e.Topic == TopicTypes.GenConfigTopic(specificTopic))
            {
                // If topic is configuration topic, check for which specific config topic is sent
                switch (specificTopic)
                {
                    case TopicTypes.UsernameUniquenessCheck:
                        if (e.Message == this.username)
                        {
                            this.mqttHandler.publish(
                                TopicTypes.GenConfigTopic($"{TopicTypes.UsernameUniquenessCheck}/{this.username}"),
                                $"Username \"{this.username}\" is taken."
                            );
                        }
                        break;

                    case TopicTypes.TopicSyncRequest:
                        if (this.connectedRooms.ContainsKey(e.Message))
                        {
                            var TopicJSON = connectedRooms[e.Message].SerializeJSON();
                            this.mqttHandler.publish(TopicTypes.GenConfigTopic(TopicTypes.TopicSyncResponse), TopicJSON);
                        }
                        break;

                    case TopicTypes.TopicSyncResponse:
                        TopicRoom room = TopicRoom.DeserializeJSON(e.Message);
                        var topic = room.Topic;
                        
                        if (this.connectedRooms.ContainsKey(topic) 
                            // Only replace if the response message contains an older version of the room:
                            && connectedRooms[topic].JoinTime > room.JoinTime)
                        {
                            this.connectedRooms[topic] = room;
                            if (topic == activeTopic)
                            {
                                ReloadMessageView();
                            }
                        }
                        break;
                }
            }
            else if (e.Topic == TopicTypes.GenMessageTopic(specificTopic))
            {
                // When message received, message is added to the list of the corresponding topic:
                connectedRooms[specificTopic].Messages.Add(e.Message);
                // UI is only updated if the message belongs to the active topic
                if (activeTopic == specificTopic)
                    AddMessageToMessageView(e.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddTopicAndSetActiveAndRequestSync(TopicTypes.DefaultMessagingTopic);
            SubscribeToMessages(TopicTypes.DefaultMessagingTopic);

            SubscribeToConfig();
        }

        private void SubscribeToConfig()
        {
            this.mqttHandler.subscribe($"{TopicTypes.GenConfigTopic()}/#");
        }

        private void PublishButtonClick(object sender, RoutedEventArgs e)
        {
            var msg = GenerateMessageText(msg_textbox.Text);
            mqttHandler.publish(TopicTypes.GenMessageTopic(activeTopic), msg);
            msg_textbox.Text = "";
        }
        private void msg_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PublishButtonClick(sender, e);
            }
        }

        private string GenerateMessageText(string content)
        {
            var time = DateTime.Now;
            string msg = $"[{time.ToString("T")}] {username}: {content}";
            return msg;
        }

        /// <summary>
        /// Subscribes to the given topic using the mqtt handler.
        /// </summary>
        private void SubscribeToMessages(string topic)
        {
            this.mqttHandler.subscribe(TopicTypes.GenMessageTopic(topic));
        }


        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttHandler.disconnect();
        }

        private void TopicAddTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            var topic = topicAddTextbox.Text;
            if (e.Key == Key.Enter)
            {
                topicAddTextbox.Text = string.Empty;
                if (!connectedRooms.ContainsKey(topic) && topic.Length > 0)
                {
                    AddTopicAndSetActiveAndRequestSync(topic);
                    SubscribeToMessages(topic);
                }
                else if (topic.Length > 0)
                {
                    MessageBox.Show("Topic already exists.");
                }
            }
        }

        /// <summary>
        /// Adds a topic to the UI listbox, sets the topic as active and requests synchronization of the topic.
        /// Should only be used once on the creation of the topic.
        /// </summary>
        private void AddTopicAndSetActiveAndRequestSync(string topic)
        {
            connectedRooms[topic] = new TopicRoom(topic);
            
            this.Dispatcher.Invoke(() =>
            {
                subscribedTopicsListBox.Items.Add(topic);
                subscribedTopicsListBox.SelectedIndex = subscribedTopicsListBox.Items.Count - 1;

                // Topic sync request is sent. The client will update the topic async if a sync response is received.
                this.mqttHandler.publish(TopicTypes.GenConfigTopic(TopicTypes.TopicSyncRequest), topic);
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
                foreach (var msg in connectedRooms[activeTopic].Messages)
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
