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
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PlayPcmWin {
    public partial class ChannelMappingSettings : Window {
        private bool mInitialized = false;

        public List<Tuple<int,int>> ChannelMapping = new List<Tuple<int, int>>();

        private static int NumOfChannelsToSelectedChannelIdx(int ch) {
            switch (ch) {
            case 2: return 0;
            case 4: return 1;
            case 6: return 2;
            case 8: return 3;
            case 10: return 4;

            case 16: return 5;
            case 18: return 6;
            case 24: return 7;
            case 26: return 8;
            case 32: return 9;

            default: return -1;
            }
        }

        private ObservableCollection<ChannelMap> mChannelMapListAll = new ObservableCollection<ChannelMap>() {
                new ChannelMap() { Title = "1 (L) →", FromCh=0, SelectedChannelIdx = 0, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "2 (R) →", FromCh=1, SelectedChannelIdx = 1, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "3 →", FromCh=2, SelectedChannelIdx = 2, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "4 →", FromCh=3, SelectedChannelIdx = 3, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "5 →", FromCh=4, SelectedChannelIdx = 4, Visibility = Visibility.Visible },

                new ChannelMap() { Title = "6 →", FromCh=5, SelectedChannelIdx = 5, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "7 →", FromCh=6, SelectedChannelIdx = 6, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "8 →", FromCh=7, SelectedChannelIdx = 7, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "9 →", FromCh=8, SelectedChannelIdx = 8, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "10 →", FromCh=9, SelectedChannelIdx = 9, Visibility = Visibility.Visible },
                
                new ChannelMap() { Title = "11 →", FromCh=10, SelectedChannelIdx = 10, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "12 →", FromCh=11, SelectedChannelIdx = 11, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "13 →", FromCh=12, SelectedChannelIdx = 12, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "14 →", FromCh=13, SelectedChannelIdx = 13, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "15 →", FromCh=14, SelectedChannelIdx = 14, Visibility = Visibility.Visible },
                
                new ChannelMap() { Title = "16 →", FromCh=15, SelectedChannelIdx = 15, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "17 →", FromCh=16, SelectedChannelIdx = 16, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "18 →", FromCh=17, SelectedChannelIdx = 17, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "19 →", FromCh=18, SelectedChannelIdx = 18, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "20 →", FromCh=19, SelectedChannelIdx = 19, Visibility = Visibility.Visible },
                
                new ChannelMap() { Title = "21 →", FromCh=20, SelectedChannelIdx = 20, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "22 →", FromCh=21, SelectedChannelIdx = 21, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "23 →", FromCh=22, SelectedChannelIdx = 22, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "24 →", FromCh=23, SelectedChannelIdx = 23, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "25 →", FromCh=24, SelectedChannelIdx = 24, Visibility = Visibility.Visible },
                
                new ChannelMap() { Title = "26 →", FromCh=25, SelectedChannelIdx = 25, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "27 →", FromCh=26, SelectedChannelIdx = 26, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "28 →", FromCh=27, SelectedChannelIdx = 27, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "29 →", FromCh=28, SelectedChannelIdx = 28, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "30 →", FromCh=29, SelectedChannelIdx = 29, Visibility = Visibility.Visible },
                
                new ChannelMap() { Title = "31 →", FromCh=30, SelectedChannelIdx = 30, Visibility = Visibility.Visible },
                new ChannelMap() { Title = "32 →", FromCh=31, SelectedChannelIdx = 31, Visibility = Visibility.Visible },
            };

        private ObservableCollection<ChannelMap> mChannelRouteList;

        public ChannelMappingSettings() {
            InitializeComponent();

            labelNumOfChannels.Content    = Properties.Resources.ChannelMappingNumOfInputChannels;
            groupBoxChannelMapping.Header = Properties.Resources.ChannelMappingChannelMappingTable;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

        public void UpdateChannelMapping(List<Tuple<int,int>> channelMapping) {
            int idx = channelMapping == null ? -1 : NumOfChannelsToSelectedChannelIdx(channelMapping.Count);
            if (idx < 0 || channelMapping == null) {
                channelMapping = new List<Tuple<int, int>> {
                    new Tuple<int,int>(0, 0),
                    new Tuple<int,int>(1, 1) };
            }

            ChannelMapping = channelMapping;

            comboBoxNumOfChannels.SelectedIndex = NumOfChannelsToSelectedChannelIdx(channelMapping.Count);
            ChannelMappingUpdated();
        }

        private void ChannelMappingUpdated() {
            ChannelMap.NumChannels = ChannelMapping.Count;

            mChannelRouteList = new ObservableCollection<ChannelMap>();
            for (int i=0; i < ChannelMap.NumChannels; ++i) {
                mChannelMapListAll[i].SelectedChannelIdx = ChannelMapping[i].Item2;
                mChannelRouteList.Add(mChannelMapListAll[i]);
            }

            listBoxRouting.DataContext = mChannelRouteList;
        }

        private void comboBoxNumOfChannels_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            int numChannels;
            bool rv = Int32.TryParse((string)comboBoxNumOfChannels.SelectedValue, out numChannels);
            if (!rv) {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (numChannels != ChannelMapping.Count) {
                ChannelMapping = new List<Tuple<int, int>>();
                for (int i=0; i < numChannels; ++i) {
                    ChannelMapping.Add(new Tuple<int, int>(i, i));
                }
            }

            ChannelMappingUpdated();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            ChannelMapping = new List<Tuple<int, int>>();

            for (int i=0; i < ChannelMap.NumChannels; ++i) {
                Console.WriteLine("{0}→{1}", i, mChannelRouteList[i].SelectedChannelIdx);
                ChannelMapping.Add(new Tuple<int, int>(i, mChannelRouteList[i].SelectedChannelIdx));
            }

            DialogResult = true;
            Close();
        }
    }

    class ChannelEntry {
        public int Id { get; set; }
        public string Name { get; set; }
    };

    class ChannelMap : INotifyPropertyChanged {
        public int SelectedChannelIdx { get; set; }
        public static int NumChannels { get; set; }
        public Visibility Visibility { get; set; }

        public Visibility Visibility2 { get { return (NumChannels == 2) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility4 { get { return (NumChannels == 4) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility6 { get { return (NumChannels == 6) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility8 { get { return (NumChannels == 8) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility10 { get { return (NumChannels == 10) ? Visibility.Visible : Visibility.Collapsed; } }

        public Visibility Visibility16 { get { return (NumChannels == 16) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility18 { get { return (NumChannels == 18) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility24 { get { return (NumChannels == 24) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility26 { get { return (NumChannels == 26) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility Visibility32 { get { return (NumChannels == 32) ? Visibility.Visible : Visibility.Collapsed; } }

        public int FromCh { get; set; }
        private string title;
        public string Title {
            get { return title; }
            set {
                title = value;
                this.NotifyPropertyChanged("Title");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
