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
        private bool[] mChannelSelectFlags = new bool[32];

        public bool[] SelectedChannels { get { return mChannelSelectFlags; } }

        public ChannelSelect() {
            InitializeComponent();
        }
        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            if (checkBox1.IsChecked == true) {
                mChannelSelectFlags[0] = true;
            }
            if (checkBox2.IsChecked == true) {
                mChannelSelectFlags[1] = true;
            }
            if (checkBox3.IsChecked == true) {
                mChannelSelectFlags[2] = true;
            }
            if (checkBox4.IsChecked == true) {
                mChannelSelectFlags[3] = true;
            }
            if (checkBox5.IsChecked == true) {
                mChannelSelectFlags[4] = true;
            }

            if (checkBox6.IsChecked == true) {
                mChannelSelectFlags[5] = true;
            }
            if (checkBox7.IsChecked == true) {
                mChannelSelectFlags[6] = true;
            }
            if (checkBox8.IsChecked == true) {
                mChannelSelectFlags[7] = true;
            }
            if (checkBox9.IsChecked == true) {
                mChannelSelectFlags[8] = true;
            }
            if (checkBox10.IsChecked == true) {
                mChannelSelectFlags[9] = true;
            }

            if (checkBox11.IsChecked == true) {
                mChannelSelectFlags[10] = true;
            }
            if (checkBox12.IsChecked == true) {
                mChannelSelectFlags[11] = true;
            }
            if (checkBox13.IsChecked == true) {
                mChannelSelectFlags[12] = true;
            }
            if (checkBox14.IsChecked == true) {
                mChannelSelectFlags[13] = true;
            }
            if (checkBox15.IsChecked == true) {
                mChannelSelectFlags[14] = true;
            }
            
            if (checkBox16.IsChecked == true) {
                mChannelSelectFlags[15] = true;
            }
            if (checkBox17.IsChecked == true) {
                mChannelSelectFlags[16] = true;
            }
            if (checkBox18.IsChecked == true) {
                mChannelSelectFlags[17] = true;
            }
            if (checkBox19.IsChecked == true) {
                mChannelSelectFlags[18] = true;
            }
            if (checkBox20.IsChecked == true) {
                mChannelSelectFlags[19] = true;
            }
            
            if (checkBox21.IsChecked == true) {
                mChannelSelectFlags[20] = true;
            }
            if (checkBox22.IsChecked == true) {
                mChannelSelectFlags[21] = true;
            }
            if (checkBox23.IsChecked == true) {
                mChannelSelectFlags[22] = true;
            }
            if (checkBox24.IsChecked == true) {
                mChannelSelectFlags[23] = true;
            }
            if (checkBox25.IsChecked == true) {
                mChannelSelectFlags[24] = true;
            }
            
            if (checkBox26.IsChecked == true) {
                mChannelSelectFlags[25] = true;
            }
            if (checkBox27.IsChecked == true) {
                mChannelSelectFlags[26] = true;
            }
            if (checkBox28.IsChecked == true) {
                mChannelSelectFlags[27] = true;
            }
            if (checkBox29.IsChecked == true) {
                mChannelSelectFlags[28] = true;
            }
            if (checkBox30.IsChecked == true) {
                mChannelSelectFlags[29] = true;
            }
            
            if (checkBox31.IsChecked == true) {
                mChannelSelectFlags[30] = true;
            }
            if (checkBox32.IsChecked == true) {
                mChannelSelectFlags[31] = true;
            }

            DialogResult = true;
            Close();
        }
    }
}
