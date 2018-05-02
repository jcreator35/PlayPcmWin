using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlayPcmWin {
    /// <summary>
    /// Interaction logic for PPWServerSettingsWindow.xaml
    /// </summary>
    public partial class PPWServerSettingsWindow : Window {
        public PPWServerSettingsWindow() {
            InitializeComponent();
        }

        public enum ServerState{
            Started,
            Stopped,
        }

        public void SetServerState(ServerState s, string ipaddr, int port) {
            switch (s) {
            case ServerState.Started:
                buttonStartServer.IsEnabled = false;
                buttonStopServer.IsEnabled = true;
                tbStatus.Text = string.Format("PPWServer is currently running on:\n    ・IP address = {0}\n    ・Port = {1}\nPlease use PPWRemote app to connect to.", ipaddr, port);
                break;
            case ServerState.Stopped:
                buttonStartServer.IsEnabled = true;
                buttonStopServer.IsEnabled = false;
                tbStatus.Text = string.Format("PPWServer is not running.");
                break;
            }
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void buttonStartServer_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        private void buttonStopServer_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
