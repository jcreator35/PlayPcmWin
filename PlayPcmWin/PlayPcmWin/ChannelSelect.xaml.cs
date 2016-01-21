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
    /// Interaction logic for ChannelSelect.xaml
    /// </summary>
    public partial class ChannelSelect : Window {

        public int SelectedChannel { get; set; }

        private bool mInitialized = false;

        public ChannelSelect() {
            InitializeComponent();
            SelectedChannel = -1;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }
        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            if (radioButton1.IsChecked == true) {
                SelectedChannel = 0;
            }
            if (radioButton2.IsChecked == true) {
                SelectedChannel = 1;
            }
            if (radioButton3.IsChecked == true) {
                SelectedChannel = 2;
            }
            if (radioButton4.IsChecked == true) {
                SelectedChannel = 3;
            }
            if (radioButton5.IsChecked == true) {
                SelectedChannel = 4;
            }
            if (radioButton6.IsChecked == true) {
                SelectedChannel = 5;
            }
            if (radioButton7.IsChecked == true) {
                SelectedChannel = 6;
            }
            if (radioButton8.IsChecked == true) {
                SelectedChannel = 7;
            }
            if (radioButton9.IsChecked == true) {
                SelectedChannel = 8;
            }
            if (radioButton10.IsChecked == true) {
                SelectedChannel = 9;
            }
            if (radioButton11.IsChecked == true) {
                SelectedChannel = 10;
            }
            if (radioButton12.IsChecked == true) {
                SelectedChannel = 11;
            }
            if (radioButton13.IsChecked == true) {
                SelectedChannel = 12;
            }
            if (radioButton14.IsChecked == true) {
                SelectedChannel = 13;
            }
            if (radioButton15.IsChecked == true) {
                SelectedChannel = 14;
            }
            if (radioButton16.IsChecked == true) {
                SelectedChannel = 15;
            }
            if (radioButton17.IsChecked == true) {
                SelectedChannel = 16;
            }
            if (radioButton18.IsChecked == true) {
                SelectedChannel = 17;
            }

            DialogResult = true;
            Close();
        }
    }
}
