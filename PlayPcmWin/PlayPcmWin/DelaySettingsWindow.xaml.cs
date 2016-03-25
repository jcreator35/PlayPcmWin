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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DelaySettingsWindow : Window {
        public DelaySettingsWindow() {
            InitializeComponent();
        }

        private string mDelayString;
        public string DelayString { get { return mDelayString; } }

        /// <summary>
        /// returns delay seconds on specified channel
        /// </summary>
        /// <param name="ch">0 to 31</param>
        /// <returns>delay (seconds)</returns>
        public double DelaySeconds(int ch) {
            double v = 0.0;
            bool rv = true;
            switch (ch) {
            case 0:
                rv = Double.TryParse(textBox1ch.Text, out v);
                break;
            case 1:
                rv = Double.TryParse(textBox2ch.Text, out v);
                break;
            case 2:
                rv = Double.TryParse(textBox3ch.Text, out v);
                break;
            case 3:
                rv = Double.TryParse(textBox4ch.Text, out v);
                break;
            case 4:
                rv = Double.TryParse(textBox5ch.Text, out v);
                break;

            case 5:
                rv = Double.TryParse(textBox6ch.Text, out v);
                break;
            case 6:
                rv = Double.TryParse(textBox7ch.Text, out v);
                break;
            case 7:
                rv = Double.TryParse(textBox8ch.Text, out v);
                break;
            case 8:
                rv = Double.TryParse(textBox9ch.Text, out v);
                break;
            case 9:
                rv = Double.TryParse(textBox10ch.Text, out v);
                break;

            case 10:
                rv = Double.TryParse(textBox11ch.Text, out v);
                break;
            case 11:
                rv = Double.TryParse(textBox12ch.Text, out v);
                break;
            case 12:
                rv = Double.TryParse(textBox13ch.Text, out v);
                break;
            case 13:
                rv = Double.TryParse(textBox14ch.Text, out v);
                break;
            case 14:
                rv = Double.TryParse(textBox15ch.Text, out v);
                break;

            case 15:
                rv = Double.TryParse(textBox16ch.Text, out v);
                break;
            case 16:
                rv = Double.TryParse(textBox17ch.Text, out v);
                break;
            case 17:
                rv = Double.TryParse(textBox18ch.Text, out v);
                break;
            case 18:
                rv = Double.TryParse(textBox19ch.Text, out v);
                break;
            case 19:
                rv = Double.TryParse(textBox20ch.Text, out v);
                break;

            case 20:
                rv = Double.TryParse(textBox21ch.Text, out v);
                break;
            case 21:
                rv = Double.TryParse(textBox22ch.Text, out v);
                break;
            case 22:
                rv = Double.TryParse(textBox23ch.Text, out v);
                break;
            case 23:
                rv = Double.TryParse(textBox24ch.Text, out v);
                break;
            case 24:
                rv = Double.TryParse(textBox25ch.Text, out v);
                break;

            case 25:
                rv = Double.TryParse(textBox26ch.Text, out v);
                break;
            case 26:
                rv = Double.TryParse(textBox27ch.Text, out v);
                break;
            case 27:
                rv = Double.TryParse(textBox28ch.Text, out v);
                break;
            case 28:
                rv = Double.TryParse(textBox29ch.Text, out v);
                break;
            case 29:
                rv = Double.TryParse(textBox30ch.Text, out v);
                break;

            case 30:
                rv = Double.TryParse(textBox31ch.Text, out v);
                break;
            case 31:
                rv = Double.TryParse(textBox32ch.Text, out v);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            if (!rv) {
                MessageBox.Show(string.Format("Error channel {0} must be number", ch+1));
            }
            return v * 0.001;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            var sb = new StringBuilder();

            for (int i = 0; i < 32; ++i) {
                double v = DelaySeconds(i);
                if (i != 0) {
                    sb.Append(",");
                }

                sb.Append(v);
            }
            mDelayString = sb.ToString();

            DialogResult = true;
            Close();
        }
    }
}
