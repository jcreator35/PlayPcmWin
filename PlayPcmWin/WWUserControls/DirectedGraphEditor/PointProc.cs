using System.Windows.Controls;
using System.Windows.Shapes;
using WWMath;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;

namespace WWUserControls {
    class PointProc {
        DrawParams mDP;
        public PointProc(DrawParams dp) {
            mDP = dp;
        }

        public List<PointInf> mPointList = new List<PointInf>();

        public PointInf mFromPoint = null;
        public PointInf mToPoint = null;

        /// <summary>
        /// FromPoint��Tmp�_�̂Ƃ������B
        /// FromPoint���m��_�̂Ƃ��m��F�ɂ���B
        /// </summary>
        public void TmpFromPointRemove() {
            if (mFromPoint != null) {
                if (PointExists(mPointList, mFromPoint.xy)) {
                    // mFromPoint�n�_�Ɋm��̓_������B
                    // �F���m��̐F�ɕύX�B
                    PointChangeColor(mFromPoint, mDP.mBrush);
                } else {
                    // mFromPoint�͉��̓_�ŁA�m��̓_�͖����B
                    PointDrawableRemove(mFromPoint);
                }

                mFromPoint = null;
            }
        }

        /// <summary>
        /// FromPoint�����݂���Ƃ��F��ύX����B
        /// </summary>
        public void FromPointChangeColor(Brush b) {
            if (mFromPoint != null) {
                PointChangeColor(mFromPoint, b);
            }
        }

        /// <summary>
        /// mToPoint�������B
        /// mToPoint�͏�ɉ��̓_�B
        /// </summary>
        public void TmpToPointRemove() {
            PointDrawableRemove(mToPoint);
            mToPoint = null;
        }

        /// <summary>
        /// �_�̕`�敨���L�����o�X����폜���A�_�̕`�敨���̂��폜�B
        /// </summary>
        public void PointDrawableRemove(PointInf p) {
            if (p == null) {
                return;
            }
            if (p.tbIdx != null) {
                mDP.mCanvas.Children.Remove(p.tbIdx);
                p.tbIdx = null;
            }

            if (p.circle != null) {
                mDP.mCanvas.Children.Remove(p.circle);
                p.circle = null;
            }

            if (p.earthCircle != null) {
                mDP.mCanvas.Children.Remove(p.earthCircle);
                p.earthCircle = null;
            }

            Console.WriteLine("Point drawable removed");
        }

        /// <summary>
        /// �_�̕`�敨�����A�L�����o�X�ɒǉ�����B
        /// </summary>
        public void PointDrawableCreate(PointInf pi, Brush brush) {
            {
                System.Diagnostics.Debug.Assert(pi.circle == null);

                pi.circle = new Ellipse();
                pi.circle.Width = mDP.mPointSz;
                pi.circle.Height = mDP.mPointSz;
                pi.circle.Fill = brush;

                Canvas.SetLeft(pi.circle, pi.xy.X - mDP.mPointSz / 2);
                Canvas.SetTop(pi.circle, pi.xy.Y - mDP.mPointSz / 2);

                mDP.mCanvas.Children.Add(pi.circle);
            }

            if (pi.Earthed) {
                System.Diagnostics.Debug.Assert(pi.earthCircle == null);

                int margin = 3;

                pi.earthCircle = new Ellipse();
                pi.earthCircle.Width = mDP.mPointSz + margin*2;
                pi.earthCircle.Height = mDP.mPointSz + margin * 2;
                pi.earthCircle.Stroke = brush;

                Canvas.SetLeft(pi.earthCircle, pi.xy.X - mDP.mPointSz / 2 - margin);
                Canvas.SetTop(pi.earthCircle, pi.xy.Y - mDP.mPointSz / 2 - margin);

                mDP.mCanvas.Children.Add(pi.earthCircle);
            }

            {
                pi.tbIdx = new TextBlock();
                pi.tbIdx.Text = string.Format("p{0}\nb={1}", pi.Idx, pi.B);
                pi.tbIdx.FontSize = mDP.mPointFontSz;
                pi.tbIdx.Foreground = mDP.mPointTextFgBrush;
                pi.tbIdx.Background = brush;
                pi.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var tbWH = pi.tbIdx.DesiredSize;
                Canvas.SetLeft(pi.tbIdx, pi.xy.X - tbWH.Width / 2);
                Canvas.SetTop(pi.tbIdx, pi.xy.Y - tbWH.Height / 2);

                mDP.mCanvas.Children.Add(pi.tbIdx);
            }
            Console.WriteLine("Point drawable added");
        }
        
        /// <summary>
        /// �_�̌W�����ύX���ꂽ�B
        /// </summary>
        public void PointParamChanged(PointInf pi, double newB) {
            pi.tbIdx.Text = string.Format("p{0}\nb={1}", pi.Idx, newB);

            // �\���ʒu�𒲐�����B
            pi.tbIdx.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tbWH = pi.tbIdx.DesiredSize;
            Canvas.SetLeft(pi.tbIdx, pi.xy.X - tbWH.Width / 2);
            Canvas.SetTop(pi.tbIdx, pi.xy.Y - tbWH.Height / 2);
        }

        /// <summary>
        /// �_p�̕`��F��ύX�B�`�敨���Ȃ��Ƃ��͍���ăL�����o�X�ɒǉ��B
        /// </summary>
        public void PointChangeColor(PointInf p, Brush brush) {
            if (p.circle == null) {
                PointDrawableCreate(p, brush);
                return;
            }

            // �_�`�敨�L��B�F��ς���B
            p.circle.Fill = brush;
            p.tbIdx.Background = brush;
        }

        /// <summary>
        /// �V����PointInf�����A�L�����o�X�ɒǉ��B
        /// �ꎞ�I�ȓ_�p�B
        /// </summary>
        public PointInf NewPoint(WWVectorD2 pos, Brush brush) {
            var pInf = new PointInf(pos);
            PointDrawableCreate(pInf, brush);
            return pInf;
        }

        /// <summary>
        /// �O���b�h�ɃA���C������B
        /// </summary>
        public static WWVectorD2 SnapToGrid(double xD, double yD, int gridSz) {
            int x = (int)xD;
            x = ((x + gridSz / 2) / gridSz) * gridSz;

            int y = (int)yD;
            y = ((y + gridSz / 2) / gridSz) * gridSz;

            return new WWVectorD2(x, y);
        }

        /// <summary>
        /// ���ׂĂ̓_���ĕ`�悷��B
        /// �@�����_���L�����o�X����폜�B
        /// �A�V�����_���쐬�A�L�����o�X�ɒǉ��B
        /// </summary>
        public void RedrawPoints() {
            foreach (var p in mPointList) {
                PointDrawableRemove(p);
                PointDrawableCreate(p, mDP.mBrush);
            }
        }

        public void UpdateEarthedPoint(PointInf ep) {
            foreach (var p in mPointList) {
                if (p == ep) {
                    p.Earthed = true;
                } else {
                    p.Earthed = false;
                }
            }

            RedrawPoints();
        }


        // ��������������������������������������������������������������������������������������������
        // Find

        public enum FindPointMode {
            FindAll,
            FindFromPointList,
        };

        /// <summary>
        /// FindAll: �m��̓_�A�܂��͉��̓_����_��T���B
        /// FindFromPointList: �m��̓_����T���B
        /// </summary>
        public PointInf FindPointByIdx(int idx, FindPointMode fpm) {
            // �m��̓_�B
            foreach (var p in mPointList) {
                if (p.Idx == idx) {
                    return p;
                }
            }

            if (fpm == FindPointMode.FindAll) {
                // ���̓_�B
                if (mFromPoint != null && mFromPoint.Idx == idx) {
                    return mFromPoint;
                }
                if (mToPoint != null && mToPoint.Idx == idx) {
                    return mToPoint;
                }
            }

            return null;
        }

        /// <summary>
        /// ���X�g�ɓ_xy�����݂��邩�B
        /// </summary>
        private bool PointExists(List<PointInf> points, WWVectorD2 xy) {
            foreach (var p in points) {
                if (WWVectorD2.Distance(p.xy, xy) < 1) {
                    return true;
                }
            }

            return false;
        }

        /// <returns>�����̓_�Ɠ�������������_��PointInf��߂��B</returns>
        private PointInf TestHit(double x, double y, double threshold) {
            return TestHit(new WWVectorD2(x, y), threshold);
        }

        /// <returns>�����̓_�Ɠ�������������_��PointInf��߂��B</returns>
        public PointInf TestHit(WWVectorD2 xy, double threshold) {
            foreach (var p in mPointList) {
                if (WWVectorD2.Distance(p.xy, xy) < threshold) {
                    return p;
                }
            }

            return null;
        }

        // ��������������������������������������������������������������������������������������������������
        // �C�x���g�B

        public void MouseMoveUpdateTmpPoint(WWVectorD2 pos) {
            if (mFromPoint == null) {
                // �n�_�������B
                Console.WriteLine("MMP FP none");
                return;
            }

            // �n�_�L��B
            Console.WriteLine("MMP ({0:0.0} {0:0.0})", pos.X, pos.Y);

            var toPoint = TestHit(pos, mDP.mPointSz);
            if (toPoint == null) {
                // �n�_�����݂��A�}�E�X�|�C���^�ʒu�Ɋm��̏I�_�������B
                if (mToPoint != null && WWVectorD2.Distance(mToPoint.xy, pos) < 1) {
                    // �}�E�X�|�C���^�ʒu�ɉ��̏I�_mToPoint�����݁B
                    Console.WriteLine("MMP already toPoint");
                } else {
                    // ���̏I�_�ʒu���قȂ�̂ō�蒼���B
                    PointDrawableRemove(mToPoint);
                    mToPoint = null;
                    mToPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("MMP create toPoint");
                }
            } else {
                // �}�E�X�|�C���^�ʒu�Ɋm��̏I�_�����݂���B
                // ���̏I�_�͕s�v�B
                PointDrawableRemove(mToPoint);
                mToPoint = null;
            }
        }

        /// <summary>
        /// �n�_��������̏�ԂŃ}�E�X���z�o�[���Ă���B
        /// </summary>
        public void SetFirstPointMouseMove(WWVectorD2 pos) {
            var point = TestHit(pos, mDP.mPointSz);

            if (mFromPoint == null) {
                // �n�_�������B
                Console.WriteLine("SFPMM FP none");

                if (point == null) {
                    // �n�_mFromPoint�������A�}�E�X�z�o�[�ʒu�Ɋm��̓_�������B
                    // �}�E�X�|�C���^�ʒu�ɉ��̎n�_�����B
                    mFromPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("SFPMM create fromPoint");
                    return;
                }

                // �n�_�������A�}�E�X�z�o�[�ʒu�Ɋm��̓_���L��B
                // �m��̓_�̐F���n�C���C�g�F�ɕύX�B
                // mFromPoint���Z�b�g����B
                mFromPoint = point;
                PointChangeColor(mFromPoint, mDP.mBrightBrush);
                return;
            }

            // �n�_mFromPoint�L��B
            Console.WriteLine("SFPMM ({0:0.0} {0:0.0})", pos.X, pos.Y);

            if (point == null) {
                // �n�_mFromPoint�����݂��A�}�E�X�|�C���^�ʒu�Ɋm��̓_�������B

                if (WWVectorD2.Distance(mFromPoint.xy, pos) < 1) {
                    // �}�E�X�|�C���^�ʒu�ɉ��̎n�_mFromPoint�����݁B
                    Console.WriteLine("SFPMM no need to change tmp FromPoint");
                } else {
                    // ���̎n�_�ʒu���قȂ�̂ō�蒼���B
                    TmpFromPointRemove();
                    mFromPoint = NewPoint(pos, mDP.mBrightBrush);
                    Console.WriteLine("SFPMM create FromPoint");
                }
            } else {
                // �n�_mFromPoint�����݂��A�}�E�X�z�o�[�ʒu�Ɋm��̎n�_point�����݂���B
                Console.WriteLine("SFPMM remove tmp drawable and set point");

                // �n�_mFromPoint�����̓_�̂Ƃ��͏����B
                TmpFromPointRemove();

                // �}�E�X�z�o�[�ʒu�̊m��̓_��mFromPoint�ɃZ�b�g����B
                // �m��̓_�̐F���n�C���C�g�F�ɕύX�B
                // mFromPoint���Z�b�g����B
                mFromPoint = point;
                PointChangeColor(mFromPoint, mDP.mBrightBrush);
                return;
            }
        }



    };
}
