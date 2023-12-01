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
using System.Threading;

namespace MosquittoChat
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly MqttClient mqttClient;
        private readonly string username;

        // Topic threads are stored in a dictionary that ties topic names to TopicRooms
        //      Messages strings include the username of the sender
        private Dictionary<string, TopicRoom> connectedRooms = new() 
        { 
            { Topics.DefaultMessagingTopic, new TopicRoom(Topics.DefaultMessagingTopic) } 
        };

        // The active topic is the topic that is currently visible in the message view of the UI
        private string activeTopic;

        public ChatWindow(MqttClient mqttClient, string username)
        {
            this.mqttClient = mqttClient;
            this.username = username;

            this.mqttClient.MessageReceived += MessageReceivedHandler;

            this.activeTopic = Topics.DefaultMessagingTopic;

            // UI
            InitializeComponent();

            Title = "MosquittoChat | " + username;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddTopicAndSetActiveAndRequestSync(Topics.DefaultMessagingTopic);
            SubscribeToMessages(Topics.DefaultMessagingTopic);

            SubscribeToConfig();
        }

        private void MessageReceivedHandler(MqttClient.MessageEventArgs e)
        {
            var topicComponents = e.Topic.Split("/");
            var specificTopic = topicComponents.Last();

            if (e.Topic == Topics.GenConfigTopic(specificTopic))
            {
                Debug.WriteLine($"Client \"{this.username}\" received \"{specificTopic}\"");
                    // If topic is configuration topic, check for which specific config topic is sent
                    switch (specificTopic)
                {
                    case Topics.UsernameUniquenessCheck:
                        if (e.Message == this.username)
                        {
                            this.mqttClient.publish(
                                Topics.GenConfigTopic($"{Topics.UsernameUniquenessCheck}/{this.username}"),
                                $"Username \"{this.username}\" is taken."
                            );
                        }
                        break;

                    case Topics.TopicSyncRequest:
                        var messageComponents = e.Message.Split("/");
                        var syncTopic    = messageComponents[0];
                        var syncUsername = messageComponents[1];

                        if (this.connectedRooms.ContainsKey(syncTopic))
                        {
                            // Sending client's username is added to the topic room
                            if(!connectedRooms[syncTopic].Users.Contains(syncUsername))
                                connectedRooms[syncTopic].Users.Add(syncUsername);
                            ReloadUsersView();

                            // Sending room data to requesting client:
                            var TopicJSON = connectedRooms[syncTopic].SerializeJSON();
                            this.mqttClient.publish(Topics.GenConfigTopic(Topics.TopicSyncResponse), TopicJSON);
                        }
                        break;

                    case Topics.TopicSyncResponse:
                        TopicRoom room = TopicRoom.DeserializeJSON(e.Message);
                        var topic = room.Topic;
                        
                        if (this.connectedRooms.ContainsKey(topic) 
                            // Only replace room if the response message contains an older version of the room:
                            && connectedRooms[topic].CreationTime > room.CreationTime)
                        {
                            this.connectedRooms[topic] = room;
                            if (topic == activeTopic)
                            {
                                ReloadMessageView();
                                ReloadUsersView();
                            }
                        }
                        break;

                    case Topics.ClientDisconnect:
                        foreach(var r in connectedRooms.Values)
                        {
                            var disconnectingUsername = e.Message;
                            //if(r.Users.Contains(disconnectingUsername)) 
                            {
                                r.Users.Remove(disconnectingUsername);
                                ReloadUsersView();
                            }
                        }
                        break;
                }
            }
            else if (e.Topic == Topics.GenMessageTopic(specificTopic))
            {
                // When message received, message is added to the list of the corresponding topic:
                connectedRooms[specificTopic].Messages.Add(e.Message);
                // UI is only updated if the message belongs to the active topic
                if (activeTopic == specificTopic)
                    AddMessageToMessageView(e.Message);
            }
        }

        private void SubscribeToConfig()
        {
            this.mqttClient.subscribe($"{Topics.GenConfigTopic()}/#");
        }

        private void PublishButtonClick(object sender, RoutedEventArgs e)
        {
            var msg = GenerateMessageText(msg_textbox.Text);
            mqttClient.publish(Topics.GenMessageTopic(activeTopic), msg);
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
        /// Subscribes to the given topic using the mqtt client.
        /// </summary>
        private void SubscribeToMessages(string topic)
        {
            this.mqttClient.subscribe(Topics.GenMessageTopic(topic));
        }


        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Debug.WriteLine("Disconnecting from client and shutting down.");
            mqttClient.publish(Topics.GenConfigTopic(Topics.ClientDisconnect), this.username);
            // Sleeping so client has time to publish:
            Thread.Sleep(MCConsts.NetworkTimeLimit);
            mqttClient.disconnect();
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
            var room = new TopicRoom(topic);
            room.Users.Add(this.username);
            connectedRooms[topic] = room;
            
            this.Dispatcher.Invoke(() =>
            {
                subscribedTopicsListBox.Items.Add(topic);
                subscribedTopicsListBox.SelectedIndex = subscribedTopicsListBox.Items.Count - 1;

                // Topic sync request is sent. The client will update the topic async if a sync response is received.
                this.mqttClient.publish(Topics.GenConfigTopic(Topics.TopicSyncRequest), $"{topic}/{this.username}");
            });

            //Setting active
            activeTopic = topic;
            ReloadUsersView();
        }

        private void AddMessageToMessageView(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                messageViewListBox.Items.Add(message);
            });
        }

        private void ReloadUsersView()
        {
            this.Dispatcher.Invoke(() =>
            {
                ConnectedUsersListBox.Items.Clear();
                foreach (var user in connectedRooms[activeTopic].Users.ToList())
                {
                    ConnectedUsersListBox.Items.Add(user);
                }
            });
        }

        private void ReloadMessageView()
        {
            this.Dispatcher.Invoke(() =>
            {
                messageViewListBox.Items.Clear();
                foreach (var msg in connectedRooms[activeTopic].Messages.ToList())
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
                ReloadUsersView();
            }
        }
    }
}
