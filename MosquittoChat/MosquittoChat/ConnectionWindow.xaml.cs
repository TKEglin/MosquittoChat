using System;
using System.Collections.Generic;
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
using System.Threading;


namespace MosquittoChat
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        private MqttHandler mqttHandler = new MqttHandler();

        public ConnectionWindow()
        {
            Debug.WriteLine("Initializing MosquittoChat");

            InitializeComponent();
        }

        private void connectButtonClick(object sender, RoutedEventArgs e)
        {
            var IP = IP_textbox.Text;
            var port = Int32.Parse(port_textbox.Text);
            var username = username_textbox.Text;

            try
            {
                this.mqttHandler.connect(IP, port);

                this.Cursor = Cursors.Wait;
                Thread.Sleep(500);

                // Verifying that messages can be published to server:
                this.mqttHandler.publish(TopicTypes.GenerateConfigTopic(TopicTypes.ConnectionCheck), username);

                // username uniqueness test
                bool usernameValid = true;
                this.mqttHandler.subscribe(TopicTypes.GenerateConfigTopic($"{TopicTypes.UsernameUniquenessCheck}/{username}"));
                this.mqttHandler.MessageReceived += e =>
                {
                    //Assumptions:
                    // 1. mqttHandler is only subscribed to the UniqueCheck/username topic above
                    // 2. A message will only be published to this topic if the username is taken

                    usernameValid = false; // Note: potential threading problems
                };
                this.mqttHandler.publish(TopicTypes.GenerateConfigTopic(TopicTypes.UsernameUniquenessCheck), username);

                // Waiting a certain amount of time for a response
                //      Note: Better solution would be to wait for event with a timeout.
                Thread.Sleep(500);
                this.Cursor = null;

                if(!usernameValid)
                {
                    this.mqttHandler.unsubscribe($"{TopicTypes.UsernameUniquenessCheck}/{username}");
                    throw new Exception("Username is taken. Please choose another username.");
                }

                // Initializing client
                var chatWindow = new ChatWindow(this.mqttHandler, username);
                chatWindow.Show();

                // Normal operation:
                //this.Close();

                // Multiclient testing:
                this.mqttHandler = new MqttHandler();
            }
            catch(Exception ex)
            { 
                MessageBox.Show($"There was a problem connecting to the server. Please try again.\n\nError: {ex.Message}");

                if (this.mqttHandler.IsConnected)
                    this.mqttHandler.disconnect();
            }
        }

        private void closeButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void windowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
