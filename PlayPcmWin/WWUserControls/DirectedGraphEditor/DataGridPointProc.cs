using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace WWUserControls {
    class DataGridPointProc {
        private DataGrid mDG;

        public class PointProperty {
            public PointInf p;
            public string Name { get { return string.Format("p{0}", p.Idx); } }

            public PointProperty(PointInf aP) {
                p = aP;
            }
        }

        ObservableCollection<PointProperty> mPointCollection = new ObservableCollection<PointProperty>();

        public delegate void PointPropertyChangedCB(PointInf p);

        private PointPropertyChangedCB mCB;

        public DataGridPointProc(DataGrid dg, PointPropertyChangedCB callback) {
            mDG = dg;
            dg.DataContext = mPointCollection;
            mCB = callback;
        }

        public void PointAdded(PointInf p) {
            mPointCollection.Add(new PointProperty(p));
        }

        public void PointRemoved(PointInf p) {
            foreach (var item in mPointCollection) {
                if (item.p == p) {
                    mPointCollection.Remove(item);
                    return;
                }
            }
        }

        public void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            if (e.EditAction == DataGridEditAction.Commit) {
                var column = e.Column as DataGridBoundColumn;
                if (column != null) {
                    int rowIndex = e.Row.GetIndex();
                    var ep = mPointCollection[rowIndex];
                    var el = e.EditingElement as TextBox;
                    /*
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    if (bindingPath == "B") {
                        double v;
                        if (double.TryParse(el.Text, out v)) {
                            if (mCB != null) {
                                mCB(ep.p, v);
                            }
                        }
                    }
                    */
                }
            }
        }
    };
}
