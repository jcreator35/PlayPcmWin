using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWShowUSBDeviceTree {
    [DebuggerDisplay("{idx} {layer} {mText}")]
    public class Module {
        public int idx;

        // Endpointの時。
        public int ifNr;
        public int altSet;
        public int endpointAddr;

        public double W { get; private set; }
        public double H { get; private set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double fontSize = 12;

        public string mText;

        public List<int> mLeftItems = new List<int>();
        public List<int> mRightItems = new List<int>();
        public List<int> mTopItems = new List<int>();
        public List<int> mBottomItems = new List<int>();

        public List<Module> mLeftModules = new List<Module>();
        public List<Module> mRightModules = new List<Module>();
        public List<Module> mTopModules = new List<Module>();
        public List<Module> mBottomModules = new List<Module>();

        public int layer;

        public void AddToLeft(Module m) {
            if (m == null || mLeftModules.Contains(m)) {
                return;
            }
            mLeftModules.Add(m);
            if (!m.mRightModules.Contains(this)) {
                m.mRightModules.Add(this);
            }
        }
        public void AddToRight(Module m) {
            if (m == null || mRightModules.Contains(m)) {
                return;
            }
            mRightModules.Add(m);
            if (!m.mLeftModules.Contains(this)) {
                m.mLeftModules.Add(this);
            }
        }
        public void AddToTop(Module m) {
            if (m == null || mTopModules.Contains(m)) {
                return;
            }
            mTopModules.Add(m);
            if (!m.mBottomModules.Contains(this)) {
                m.mBottomModules.Add(this);
            }
        }
        public void AddToBottom(Module m) {
            if (m == null || mBottomModules.Contains(m)) {
                return;
            }

            mBottomModules.Add(m);
            if (!m.mTopModules.Contains(this)) {
                m.mTopModules.Add(this);
            }
        }

        public void ConnectToBottomItem(int id) {
            if (id <= 0) {
                return;
            }
            mBottomItems.Add(id);
        }

        public void ConnectToTopItem(int id) {
            if (id <= 0) {
                return;
            }
            mTopItems.Add(id);
        }

        public void ConnectToLeftItem(int id) {
            if (id <= 0) {
                return;
            }
            mLeftItems.Add(id);
        }
        public void ConnectToRightItem(int id) {
            if (id <= 0) {
                return;
            }
            mRightItems.Add(id);
        }

        public UIElement uiElement;

        public void UpdateText(string text) {
            mText = text;
            CreateUIElement();
        }

        public void CreateUIElement() {
            var tb = new TextBlock {
                Padding = new Thickness(4),
                Text = mText,
                FontSize = fontSize,
                //TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(0x43, 0x43, 0x47)),
                Foreground = new SolidColorBrush(Colors.White),
            };
            var bd = new Border() {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Colors.White),
                Child = tb,
            };

            // UI Elementのサイズを確定します。ActualWidthとActualHeightで取得できる。
            bd.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            bd.Arrange(new Rect(bd.DesiredSize));
            W = bd.ActualWidth;
            H = bd.ActualHeight;

            uiElement = bd;
        }

        public Module(int aIdx, string text) {
            idx = aIdx;
            mText = text;
            CreateUIElement();
        }

        /// <summary>
        /// layerを確定する。
        /// </summary>
        public void ResolveLayer() {
            layer = 0;
            Module c = this;
            while (c.mLeftModules.Count != 0) {
                c = c.mLeftModules[0];
                ++layer;
            }
        }
    }
}
