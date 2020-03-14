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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WWUserControls {
    public partial class Function1dGraph : UserControl {
        public Function1dGraph() {
            InitializeComponent();
        }

        public string Title {
            get { return (string)labelTitle.Content; }
            set { labelTitle.Content = value; }
        }

        public string YAxis {
            get { return (string)labelY.Content; }
            set { labelY.Content = value; }
        }

        public string XAxis {
            get { return (string)labelX.Content; }
            set { labelX.Content = value; }
        }
    }
}
