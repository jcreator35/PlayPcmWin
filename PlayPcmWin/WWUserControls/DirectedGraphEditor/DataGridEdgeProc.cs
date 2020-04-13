using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace WWUserControls {
    class DataGridEdgeProc {
        private DataGrid mDG;

        public class EdgeProperty {
            public Edge edge;
            public string Name { get { return string.Format("e{0}", edge.EdgeIdx); } }
            public double C { get { return edge.C; } set { edge.C = value;} }
            public double B { get { return edge.B; } set { edge.B = value; } }
            public double F { get { return edge.F; } set { edge.F = value; } }

            public EdgeProperty(Edge e) {
                edge = e;
            }
        }

        ObservableCollection<EdgeProperty> mEdgeCollection = new ObservableCollection<EdgeProperty>();

        public delegate void EdgePropertyChangedCB(Edge e, double newC, double newB, double newF);

        private EdgePropertyChangedCB mCB;

        public DataGridEdgeProc(DataGrid dg, EdgePropertyChangedCB callback) {
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
                    int rowIndex = e.Row.GetIndex();
                    var ep = mEdgeCollection[rowIndex];
                    var el = e.EditingElement as TextBox;

                    var bindingPath = (column.Binding as Binding).Path.Path;
                    if (bindingPath == "C") {
                        double c;
                        if (double.TryParse(el.Text, out c)) {
                            if (mCB != null) {
                                mCB(ep.edge, c, ep.B, ep.F);
                            }
                        }
                    }

                    if (bindingPath == "B") {
                        double b;
                        if (double.TryParse(el.Text, out b)) {
                            if (mCB != null) {
                                mCB(ep.edge, ep.C, b, ep.F);
                            }
                        }
                    }
                    if (bindingPath == "F") {
                        double f;
                        if (double.TryParse(el.Text, out f)) {
                            if (mCB != null) {
                                mCB(ep.edge, ep.C, ep.B, f);
                            }
                        }
                    }
                }
            }
        }
    };
}
