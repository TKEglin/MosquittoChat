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


namespace MosquittoChat
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {

        private readonly MqttHandler mqttHandler = new MqttHandler();

        public ConnectionWindow()
        {
            Debug.WriteLine("Initializing MosquittoChat");

            InitializeComponent();

        }

        private void connectButtonClick(object sender, RoutedEventArgs e)
        {
            var IP = IP_textbox.Text;
            var port = Int32.Parse(port_textbox.Text);

            this.mqttHandler.connect(IP, port);

            var chatWindow = new ChatWindow(this.mqttHandler);
            chatWindow.Show();
            this.Close();
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
