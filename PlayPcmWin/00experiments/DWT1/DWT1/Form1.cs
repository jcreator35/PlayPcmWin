using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DWT1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pictureBoxSource.Image = null;
            pictureBoxDWTed.Image = null;
            if (!LoadSignal(SIGNAL_FILENAME))
            {
                PrepareSourceData();
            }
            TrackbarMagnitudeUpdated();
            TrackbarOffsetUpdated();
            UpdateGui();
        }

        private const int SRC_W = 1024;
        private const int SRC_LOG2_W = 10;
        private const int SRC_H = 257;
        private const int DWT_H = 256;
        private const int SRC_HALF_H = 128;
        private const string SIGNAL_FILENAME = "sourceSignal.txt";
        private float[] sourceSignal = null;
        private float[] dwtData = null;
        private float[] processedSignal = null;
        private int offset = 0;
        private const int OFFSET_MAX_PLUS_1 = 128;
        private int magnitude = 256;

        private void PrepareSourceData()
        {
            sourceSignal = new float[SRC_W + OFFSET_MAX_PLUS_1];
            for (int x = 0; x < SRC_W; ++x)
            {
                sourceSignal[x] = 0.5f * (float)Math.Sin(x * 0.08f);
            }
        }

        private void SaveSignal(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine("1");
                sw.WriteLine(sourceSignal.Length);

                for (int i = 0; i < sourceSignal.Length; ++i)
                {
                    sw.WriteLine(sourceSignal[i]);
                }
            }
        }

        private bool LoadSignal(string path)
        {
            bool ret = false;

            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string version = sr.ReadLine().Trim();
                    if (0 != version.CompareTo("1"))
                    {
                        return false;
                    }

                    sourceSignal = new float[SRC_W + OFFSET_MAX_PLUS_1];
                    int length = System.Int32.Parse(sr.ReadLine().Trim());
                    if (SRC_W < length)
                    {
                        length = SRC_W;
                    }

                    for (int i = 0; i < length; ++i)
                    {
                        sourceSignal[i] = (float)System.Double.Parse(sr.ReadLine().Trim());
                    }
                }
                ret = true;
            }
            catch
            {
            }
            return ret;
        }

        private void UpdateGui()
        {
            UpdateDwt();
            Effect();
            UpdateIdwt();
            UpdatePictureBoxSource();
        }

        private Point prevXY;

        private void pictureBoxSource_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
            {
                return;
            }
            if (e.X < 0 || sourceSignal.Length <= e.X + offset ||
                e.Y < 0 || SRC_H <= e.Y)
            {
                return;
            }

            if (prevXY.X < 0)
            {
                prevXY = new Point(e.X+offset, e.Y);
                return;
            }

            Point from = new Point(prevXY.X, prevXY.Y);
            Point to = new Point(e.X+offset, e.Y);
            float katamuki = 0;
            if (e.X+offset < prevXY.X)
            {
                from = new Point(e.X+offset, e.Y);
                to = new Point(prevXY.X, prevXY.Y);
            }

            if (from.X != to.X)
            {
                katamuki = ((float)to.Y - from.Y) / (to.X - from.X);
            }

            for (int i = 0; i <= to.X - from.X; ++i)
            {
                int x = from.X + i;
                float y = (float)from.Y + katamuki * i;
                sourceSignal[x] = (SRC_HALF_H - y) / SRC_HALF_H;
            }
            prevXY = new Point(e.X+offset, e.Y);
            UpdateGui();
        }

        private void pictureBoxSource_MouseEnter(object sender, EventArgs e)
        {
            prevXY = new Point(-1, -1);
        }

        private void pictureBoxSource_MouseUp(object sender, MouseEventArgs e)
        {
            prevXY = new Point(-1, -1);
        }

        private void pictureBoxSource_MouseLeave(object sender, EventArgs e)
        {
            prevXY = new Point(-1, -1);
        }

        private void pictureBoxSource_MouseDown(object sender, MouseEventArgs e)
        {
            prevXY = new Point(e.X+offset, e.Y);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveSignal(SIGNAL_FILENAME);
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            LoadSignal(SIGNAL_FILENAME);
            UpdateGui();
        }

        private void UpdatePictureBoxSource()
        {
            Bitmap bmp = new Bitmap(SRC_W, SRC_H);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(new SolidBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff)), 0, 0, SRC_W, SRC_H);

            PointF[] points = new PointF[(SRC_W - 1) * 2];

            for (int x = 0; x < SRC_W - 1; ++x)
            {
                points[x * 2] = new PointF(x, -sourceSignal[x + offset] * SRC_HALF_H + SRC_HALF_H);
                points[x * 2 + 1] = new PointF(x + 1, -sourceSignal[x + offset + 1] * SRC_HALF_H + SRC_HALF_H);
            }
            g.DrawLines(new Pen(Color.Black), points);

            for (int x = 0; x < SRC_W - 1; ++x)
            {
                points[x * 2] = new PointF(x, -dwtData[x ] * SRC_HALF_H + SRC_HALF_H);
                points[x * 2 + 1] = new PointF(x + 1, -dwtData[x + 1] * SRC_HALF_H + SRC_HALF_H);
            }
            g.DrawLines(new Pen(Color.Red), points);

            if (null != pictureBoxSource.Image)
            {
                pictureBoxSource.Image.Dispose();
            }
            pictureBoxSource.Image = bmp;
        }

        private void UpdateDwt()
        {
            Bitmap bmp = new Bitmap(SRC_W, DWT_H);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(new SolidBrush(Color.FromArgb(0xff, 0, 0, 0)), 0, 0, SRC_W, DWT_H);

            RectangleF[] rects = new RectangleF[SRC_W * 3];
            dwtData = new float[SRC_W * 11];
            Array.Copy(sourceSignal, offset, dwtData, 0, SRC_W);

            Color[] colors = new Color[sourceSignal.Length * 3];

            int readPos = 0;
            int writePos = SRC_W;
            float y = 0;
            float h = 8.0f;
            int posR = 0;

            for (int j = SRC_W / 2; 1 <= j; j /= 2)
            {
                float w = SRC_W / j;

                for (int i = 0; i < j; ++i)
                {
                    float a = (dwtData[readPos + i * 2] + dwtData[readPos + i * 2 + 1]) * 0.5f;
                    float d = dwtData[readPos + i * 2] - a;
                    dwtData[writePos + i] = a;
                    dwtData[writePos + j + i] = d;

                    rects[posR] = new RectangleF(i * w, y, w, h);

                    int r = (int)((0 < d) ? d * magnitude : 0);
                    if (255 < r) { r = 255; }
                    int b = (int)((d < 0) ? -d * magnitude : 0);
                    if (255 < b) { b = 255; }
                    colors[posR++] = Color.FromArgb(b, 0, r);
                }

                for (int i = j * 2; i < SRC_W; ++i)
                {
                    dwtData[writePos + i] = dwtData[writePos - SRC_W + i];
                }

                readPos += SRC_W;
                writePos += SRC_W;
                y += h;
            }

            for (int i = 0; i < posR; ++i)
            {
                g.FillRectangle(new SolidBrush(colors[i]), rects[i]);
            }

            if (null != pictureBoxDWTed.Image)
            {
                pictureBoxDWTed.Image.Dispose();
            }
            pictureBoxDWTed.Image = bmp;
        }

        private void UpdateIdwt()
        {
            int readAPos = SRC_W*(SRC_LOG2_W);
            int writePos = readAPos - SRC_W;
            for (int j = 1; j < SRC_W; j *= 2)
            {
                int readDPos = readAPos + j;
                for (int i = 0; i < j; ++i)
                {
                    float a = dwtData[readAPos +i];
                    float d = dwtData[readDPos +i];
                    dwtData[writePos +i*2  ] = a + d;
                    dwtData[writePos +i*2+1] = a - d;
                }
                readAPos -= SRC_W;
                writePos -= SRC_W;
            }
        }

        private void Effect()
        {
            int readPos = SRC_W * (SRC_LOG2_W);
            for (int j = 1; j < SRC_W; j *= 2)
            {
                int lastMagPos = -8;
                int readDPos = readPos + j;
                for (int i = 0; i < j - 4; ++i)
                {
                    float d0 = dwtData[readDPos + i];
                    float d1 = dwtData[readDPos + i+1];
                    float d2 = dwtData[readDPos + i+2];
                    float d3 = dwtData[readDPos + i+3];
                    float valueAbs = Math.Abs(d0 - d1 + d2 - d3);
                    if (d0 * d1 < 0 && d2 * d3 < 0 && d0 * d2 > 0)
                    {
                        if (lastMagPos + 8 < i && 0.05f < valueAbs)
                        {
                            dwtData[readDPos + i] *= 2.0f;
                            dwtData[readDPos + i + 1] *= 2.0f;
                            lastMagPos = i;
                        }
                    }
                }
                readPos -= SRC_W;
            }
        }

        private void TrackbarMagnitudeUpdated()
        {
            magnitude = (int)Math.Pow(2, trackBar1.Value + 8);
            label1.Text = string.Format("{0} x", magnitude);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            TrackbarMagnitudeUpdated();
            UpdateGui();
        }

        private void TrackbarOffsetUpdated()
        {
            offset = (int)trackBar2.Value;
            label2.Text = string.Format("offset = {0}", offset);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            TrackbarOffsetUpdated();
            UpdateGui();
        }

    }
}
