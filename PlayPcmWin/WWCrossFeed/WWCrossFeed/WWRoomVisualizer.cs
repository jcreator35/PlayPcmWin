using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace WWCrossFeed {
    class WWRoomVisualizer {
        private Canvas mCanvas;
        private WWRoom mRoom;
        private Matrix3D mWorldProjectionMatrix;
        private double mCameraNear = 1.0;
        public double CameraFovHDegree { get; set; }
        private double mCameraDistanceCurrent;

        private WWVirtualTrackball mVirtualTrackball = new WWVirtualTrackball();
        private WWCrossFeedFir mCrossFeed;

        public WWRoomVisualizer(Canvas canvas) {
            mCanvas = canvas;
            mCanvas.MouseDown += mCanvas_MouseDown;
            mCanvas.MouseMove += mCanvas_MouseMove;
            mCanvas.MouseUp += mCanvas_MouseUp;
            mCanvas.MouseWheel += mCanvas_MouseWheel;

            mVirtualTrackball.ScreenWH = new Size(mCanvas.Width, mCanvas.Height);
            mVirtualTrackball.SphereRadius = mCanvas.Width/2;
        }

        public void SetCrossFeed(WWCrossFeedFir crossFeed) {
            mCrossFeed = crossFeed;
        }

        void mCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            mCameraDistanceCurrent += -e.Delta * 0.01f;
            if (mCameraDistanceCurrent < mCameraNear * 2) {
                mCameraDistanceCurrent = mCameraNear * 2;
            }
            Redraw();
        }

        void mCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            mVirtualTrackball.Down(e.GetPosition(mCanvas));
            Mouse.Capture(mCanvas);
        }

        void mCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            mVirtualTrackball.Move(e.GetPosition(mCanvas));
            Redraw();
        }

        void mCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            mVirtualTrackball.Up();
            Mouse.Capture(null);
        }

        public void ResetCamera(double cameraDistance) {
            mCameraDistanceCurrent = cameraDistance;
            mVirtualTrackball.Reset();
        }

        private void UpdateCameraMatrix() {
            // right-handed coordinates

#if false
            // front view
            var eye = new Vector3D(0.0, 0.0, -CameraDistance);
            var at = new Vector3D(0.0, 0.0, 0.0);
            var up = new Vector3D(0.0, 1.0, 0.0);
            var lookAt = WWMatrixUtil.CalculateLookAt(eye, at, up);
#else
            // virtual trackball
            var cameraRot = mVirtualTrackball.RotationMatrix();
            var cameraTranslate = new Matrix3D();
            cameraTranslate.Translate(new Vector3D(0.0, 0.0, -mCameraDistanceCurrent));
            var cameraMat = cameraTranslate * cameraRot;
            var lookAt = cameraMat;
            lookAt.Invert();
#endif

            var viewProjectionMatrix = WWMatrixUtil.CreatePerspectiveProjectionMatrix(mCameraNear, mCameraDistanceCurrent * 2.0, CameraFovHDegree, 1.0);

            mWorldProjectionMatrix = lookAt * viewProjectionMatrix;
        }

        public void SetRoom(WWRoom room) {
            mRoom = room;
        }

        private void RedrawCrossfeed(WWCrossFeedFir crossFeed) {
            for (int i = 0; i < crossFeed.Count(); ++i) {
                DrawRoute(crossFeed.GetNth(i));
            }
        }

        public void Redraw() {
            mCanvas.Children.Clear();

            UpdateCameraMatrix();

            RedrawRoom();
            RedrawCrossfeed(mCrossFeed);
        }

        private void RedrawRoom() {
            DrawModel(mRoom.RoomModel, Matrix3D.Identity, new SolidColorBrush(Colors.White));

            Matrix3D listenerMatrix = new Matrix3D();
            listenerMatrix.Translate((Vector3D)mRoom.ListenerPos);
            DrawModel(mRoom.ListenerModel, listenerMatrix, new SolidColorBrush(Colors.Gray));

            for (int i = 0; i < WWRoom.NUM_OF_SPEAKERS; ++i) {
                var pos = mRoom.SpeakerPos(i);
                var dir = mRoom.SpeakerDir(i);
                Vector3D posV = new Vector3D(pos.X, pos.Y, pos.Z);
                Vector3D at = new Vector3D(pos.X +dir.X, pos.Y + dir.Y, pos.Z + dir.Z);
                Vector3D up = new Vector3D(0.0, 1.0, 0.0);

                Matrix3D speakerMatrixInv = WWMatrixUtil.CalculateLookAt(posV, at, up);
                var speakerMatrix = speakerMatrixInv;
                speakerMatrix.Invert();

                DrawModel(mRoom.SpeakerModel, speakerMatrix, new SolidColorBrush(Colors.Gray));
            }
        }

        private void DrawModel(WW3DModel model, Matrix3D modelWorldMatrix, Brush brush) {
            var modelProjectionMatrix = modelWorldMatrix * mWorldProjectionMatrix;

            var pointArray = model.TriangleList();
            var indexArray = model.IndexList();
            for (int i = 0; i < indexArray.Length / 3; ++i) {
                {
                    Point3D p0 = Point3D.Multiply(pointArray[indexArray[i * 3 + 0]], modelProjectionMatrix);
                    Point3D p1 = Point3D.Multiply(pointArray[indexArray[i * 3 + 1]], modelProjectionMatrix);
                    Point3D p2 = Point3D.Multiply(pointArray[indexArray[i * 3 + 2]], modelProjectionMatrix);
                    AddNewLine(p0, p1, brush);
                    AddNewLine(p1, p2, brush);
                    AddNewLine(p2, p0, brush);
                }
#if false
                // 法線のデバッグ表示
                Point3D po0 = pointArray[indexArray[i * 3 + 0]];
                Point3D po1 = pointArray[indexArray[i * 3 + 1]];
                Point3D po2 = pointArray[indexArray[i * 3 + 2]];
                var edge0 = po1 - po0;
                var edge1 = po2 - po1;
                var n = Vector3D.CrossProduct(edge0, edge1);
                n.Normalize();
                var center = new Point3D((po0.X + po1.X + po2.X)/3, (po0.Y + po1.Y + po2.Y)/3, (po0.Z + po1.Z + po2.Z)/3);
                Point3D pN0 = Point3D.Multiply(center, modelProjectionMatrix);
                Point3D pN1 = Point3D.Multiply(center+n*0.1, modelProjectionMatrix);
                AddNewLine(pN0, pN1, brush);

                // 面の番号表示
                TextBlock tb = new TextBlock();
                tb.Text = i.ToString();
                tb.Foreground = brush;
                mCanvas.Children.Add(tb);
                var textPos = ScaleToCanvas(pN0);
                Canvas.SetLeft(tb, textPos.X);
                Canvas.SetTop(tb, textPos.Y);
#endif
            }
        }

        private Vector ScaleToCanvas(Point3D v) {
            return new Vector(mCanvas.Width/2 * (v.X+1.0), mCanvas.Height/2 * (v.Y+1.0));
        }

        private static bool InRange(Point3D v) {
            if (v.X < -1.0 || 1.0 < v.X || v.Y < -1.0 || 1.0 < v.Y) {
                return false;
            }
            return true;
        }

        private void AddNewLine(Point3D p0, Point3D p1, Brush brush) {
            if (!InRange(p0) || !InRange(p1)) {
                return;
            }
            var from = ScaleToCanvas(p0);
            var to = ScaleToCanvas(p1);

            var line = new Line();
            line.X1 = from.X;
            line.Y1 = from.Y;
            line.X2 = to.X;
            line.Y2 = to.Y;
            line.Stroke = brush;

            mCanvas.Children.Add(line);
        }

        private void DrawRoute(WWRoute route) {
            Brush[] brushes = new Brush[] {
                new SolidColorBrush(Colors.Cyan),
                new SolidColorBrush(Colors.Magenta)
            };

            for (int i = 0; i < route.Count(); ++i) {
                var lineSegment = route.GetNth(i);
                Point3D p0 = Point3D.Multiply(lineSegment.StartPos, mWorldProjectionMatrix);
                Point3D p1 = Point3D.Multiply(lineSegment.StartPos + lineSegment.Length * lineSegment.Direction, mWorldProjectionMatrix);

                Brush b = brushes[route.EarCh].Clone();
                b.Opacity = IntensityToOpacity(lineSegment.Intensity);
                AddNewLine(p0, p1, b);

                Point3D pSL = Point3D.Multiply(mRoom.SpeakerPos(0), mWorldProjectionMatrix);
                Point3D pSR = Point3D.Multiply(mRoom.SpeakerPos(1), mWorldProjectionMatrix);
                AddNewLine(pSL, p1, b);
                AddNewLine(pSR, p1, b);
            }
        }

        private static double IntensityToOpacity(double intensity) {
            double opacity = Math.Pow(intensity, 0.5);
            if (1.0 < opacity) {
                opacity = 1.0;
            }
            return opacity;
        }
    }
}
