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
    public partial class ChannelRoutingSettings : Window {
        private bool mInitialized = false;

        public List<Tuple<int,int>> ChannelRouting = new List<Tuple<int, int>>();

        private static int NumOfChannelsToSelectedChannelIdx(int ch) {
            switch (ch) {
            case 2: return 0;
            case 4: return 1;
            case 6: return 2;
            case 8: return 3;
            case 10: return 4;
            case 16: return 5;
            case 18: return 6;
            default: return -1;
            }
        }

        private ObservableCollection<ChannelRoute> mChannelRouteListAll = new ObservableCollection<ChannelRoute>() {
                new ChannelRoute() { Title = "1 (L) →", FromCh=0, SelectedChannelIdx = 0, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "2 (R) →", FromCh=1, SelectedChannelIdx = 1, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "3 →", FromCh=2, SelectedChannelIdx = 2, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "4 →", FromCh=3, SelectedChannelIdx = 3, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "5 →", FromCh=4, SelectedChannelIdx = 4, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "6 →", FromCh=5, SelectedChannelIdx = 5, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "7 →", FromCh=6, SelectedChannelIdx = 6, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "8 →", FromCh=7, SelectedChannelIdx = 7, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "9 →", FromCh=8, SelectedChannelIdx = 8, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "10 →", FromCh=9, SelectedChannelIdx = 9, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "11 →", FromCh=10, SelectedChannelIdx = 10, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "12 →", FromCh=11, SelectedChannelIdx = 11, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "13 →", FromCh=12, SelectedChannelIdx = 12, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "14 →", FromCh=13, SelectedChannelIdx = 13, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "15 →", FromCh=14, SelectedChannelIdx = 14, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "16 →", FromCh=15, SelectedChannelIdx = 15, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "17 →", FromCh=16, SelectedChannelIdx = 16, Visibility = Visibility.Visible },
                new ChannelRoute() { Title = "18 →", FromCh=17, SelectedChannelIdx = 17, Visibility = Visibility.Visible },
            };

        private ObservableCollection<ChannelRoute> mChannelRouteList;

        public ChannelRoutingSettings() {
            InitializeComponent();

            labelNumOfChannels.Content    = Properties.Resources.ChannelRoutingNumOfInputChannels;
            groupBoxChannelRouting.Header = Properties.Resources.ChannelRoutingChannelRoutingTable;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

        public void UpdateChannelRouting(List<Tuple<int,int>> channelRouting) {
            int idx = channelRouting == null ? -1 : NumOfChannelsToSelectedChannelIdx(channelRouting.Count);
            if (idx < 0 || channelRouting == null) {
                channelRouting = new List<Tuple<int, int>> {
                    new Tuple<int,int>(0, 0),
                    new Tuple<int,int>(1, 1) };
            }

            ChannelRouting = channelRouting;

            comboBoxNumOfChannels.SelectedIndex = NumOfChannelsToSelectedChannelIdx(channelRouting.Count);
            ChannelRoutingUpdated();
        }

        private void ChannelRoutingUpdated() {
            ChannelRoute.NumChannels = ChannelRouting.Count;

            mChannelRouteList = new ObservableCollection<ChannelRoute>();
            for (int i=0; i < ChannelRoute.NumChannels; ++i) {
                mChannelRouteListAll[i].SelectedChannelIdx = ChannelRouting[i].Item2;
                mChannelRouteList.Add(mChannelRouteListAll[i]);
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

            if (numChannels != ChannelRouting.Count) {
                ChannelRouting = new List<Tuple<int, int>>();
                for (int i=0; i < numChannels; ++i) {
                    ChannelRouting.Add(new Tuple<int, int>(i, i));
                }
            }

            ChannelRoutingUpdated();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            ChannelRouting = new List<Tuple<int, int>>();

            for (int i=0; i < ChannelRoute.NumChannels; ++i) {
                Console.WriteLine("{0}→{1}", i, mChannelRouteList[i].SelectedChannelIdx);
                ChannelRouting.Add(new Tuple<int, int>(i, mChannelRouteList[i].SelectedChannelIdx));
            }

            DialogResult = true;
            Close();
        }
    }

    class ChannelEntry {
        public int Id { get; set; }
        public string Name { get; set; }
    };

    class ChannelRoute : INotifyPropertyChanged {
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
