using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace WWUserControls {
    class DataGridEdgeProc {
        private DataGrid mDG;

        public class EdgeProperty {
            public Edge edge;
            public string Name { get { return string.Format("e{0}", edge.EdgeIdx); } }
            public double Coef { get { return edge.coef; } set { edge.coef = value;} }

            public EdgeProperty(Edge e) {
                edge = e;
            }
        }

        ObservableCollection<EdgeProperty> mEdgeCollection = new ObservableCollection<EdgeProperty>();

        public delegate void EdgeCoeffChangedCB(Edge e, double newValue);

        private EdgeCoeffChangedCB mCB;

        public DataGridEdgeProc(DataGrid dg, EdgeCoeffChangedCB callback) {
            mDG = dg;
            dg.DataContext = mEdgeCollection;
            mCB = callback;
        }

        public void EdgeAdded(Edge edge) {
            mEdgeCollection.Add(new EdgeProperty(edge));
        }

        public void EdgeRemoved(Edge edge) {
            foreach (var e in mEdgeCollection) {
                if (e.edge == edge) {
                    mEdgeCollection.Remove(e);
                    return;
                }
            }
        }

        public void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            if (e.EditAction == DataGridEditAction.Commit) {
                var column = e.Column as DataGridBoundColumn;
                if (column != null) {
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    if (bindingPath == "Coef") {
                        int rowIndex = e.Row.GetIndex();
                        var el = e.EditingElement as TextBox;

                        var ep = mEdgeCollection[rowIndex];

                        double v;
                        if (double.TryParse(el.Text, out v)) {
                            if (mCB != null) {
                                mCB(ep.edge, v);
                            }
                        }
                    }
                }
            }
        }
    };
}
