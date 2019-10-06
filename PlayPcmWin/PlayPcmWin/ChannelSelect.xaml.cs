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
        private const int NUM_CHANNELS = 32;
        private bool[] mChannelSelectFlags = new bool[NUM_CHANNELS];
        private CheckBox[] mCheckboxList = new CheckBox[NUM_CHANNELS];

        public bool[] SelectedChannels { get { return mChannelSelectFlags; } }

        public ChannelSelect() {
            InitializeComponent();

            #region boring part
            mCheckboxList[0] = checkBox1;
            mCheckboxList[1] = checkBox2;
            mCheckboxList[2] = checkBox3;
            mCheckboxList[3] = checkBox4;
            mCheckboxList[4] = checkBox5;

            mCheckboxList[5] = checkBox6;
            mCheckboxList[6] = checkBox7;
            mCheckboxList[7] = checkBox8;
            mCheckboxList[8] = checkBox9;
            mCheckboxList[9] = checkBox10;

            mCheckboxList[10] = checkBox11;
            mCheckboxList[11] = checkBox12;
            mCheckboxList[12] = checkBox13;
            mCheckboxList[13] = checkBox14;
            mCheckboxList[14] = checkBox15;

            mCheckboxList[15] = checkBox16;
            mCheckboxList[16] = checkBox17;
            mCheckboxList[17] = checkBox18;
            mCheckboxList[18] = checkBox19;
            mCheckboxList[19] = checkBox20;

            mCheckboxList[20] = checkBox21;
            mCheckboxList[21] = checkBox22;
            mCheckboxList[22] = checkBox23;
            mCheckboxList[23] = checkBox24;
            mCheckboxList[24] = checkBox25;

            mCheckboxList[25] = checkBox26;
            mCheckboxList[26] = checkBox27;
            mCheckboxList[27] = checkBox28;
            mCheckboxList[28] = checkBox29;
            mCheckboxList[29] = checkBox30;

            mCheckboxList[30] = checkBox31;
            mCheckboxList[31] = checkBox32;
            #endregion
        }

        /// <param name="ch">zero based channel idx</param>
        public void SetChannel(int ch, bool b) {
            System.Diagnostics.Debug.Assert(0 <= ch && ch < NUM_CHANNELS);
            mCheckboxList[ch].IsChecked = b;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < NUM_CHANNELS; ++i) {
                mChannelSelectFlags[i] = mCheckboxList[i].IsChecked == true;
            }

            DialogResult = true;
            Close();
        }

    }
}
