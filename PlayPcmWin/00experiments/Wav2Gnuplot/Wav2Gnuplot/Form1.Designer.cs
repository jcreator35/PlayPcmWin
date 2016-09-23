namespace Wav2Gnuplot
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonBrowseReadWavFile = new System.Windows.Forms.Button();
            this.textBoxReadWavFile = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonBrowseWriteFile = new System.Windows.Forms.Button();
            this.textBoxWriteFile = new System.Windows.Forms.TextBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numericStartPos = new System.Windows.Forms.NumericUpDown();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.numericOutputSamples = new System.Windows.Forms.NumericUpDown();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBoxCh1 = new System.Windows.Forms.CheckBox();
            this.checkBoxCh0 = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numericUpDownSubSampleOffset = new System.Windows.Forms.NumericUpDown();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericStartPos)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericOutputSamples)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSubSampleOffset)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonBrowseReadWavFile);
            this.groupBox1.Controls.Add(this.textBoxReadWavFile);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // buttonBrowseReadWavFile
            // 
            resources.ApplyResources(this.buttonBrowseReadWavFile, "buttonBrowseReadWavFile");
            this.buttonBrowseReadWavFile.Name = "buttonBrowseReadWavFile";
            this.buttonBrowseReadWavFile.UseVisualStyleBackColor = true;
            this.buttonBrowseReadWavFile.Click += new System.EventHandler(this.buttonBrowseReadWavFile_Click);
            // 
            // textBoxReadWavFile
            // 
            resources.ApplyResources(this.textBoxReadWavFile, "textBoxReadWavFile");
            this.textBoxReadWavFile.Name = "textBoxReadWavFile";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonBrowseWriteFile);
            this.groupBox2.Controls.Add(this.textBoxWriteFile);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // buttonBrowseWriteFile
            // 
            resources.ApplyResources(this.buttonBrowseWriteFile, "buttonBrowseWriteFile");
            this.buttonBrowseWriteFile.Name = "buttonBrowseWriteFile";
            this.buttonBrowseWriteFile.UseVisualStyleBackColor = true;
            this.buttonBrowseWriteFile.Click += new System.EventHandler(this.buttonBrowseWriteFile_Click);
            // 
            // textBoxWriteFile
            // 
            resources.ApplyResources(this.textBoxWriteFile, "textBoxWriteFile");
            this.textBoxWriteFile.Name = "textBoxWriteFile";
            // 
            // buttonStart
            // 
            resources.ApplyResources(this.buttonStart, "buttonStart");
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // textBoxConsole
            // 
            resources.ApplyResources(this.textBoxConsole, "textBoxConsole");
            this.textBoxConsole.Name = "textBoxConsole";
            this.textBoxConsole.ReadOnly = true;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.numericStartPos);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // numericStartPos
            // 
            resources.ApplyResources(this.numericStartPos, "numericStartPos");
            this.numericStartPos.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.numericStartPos.Name = "numericStartPos";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Controls.Add(this.numericOutputSamples);
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // numericOutputSamples
            // 
            resources.ApplyResources(this.numericOutputSamples, "numericOutputSamples");
            this.numericOutputSamples.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.numericOutputSamples.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericOutputSamples.Name = "numericOutputSamples";
            this.numericOutputSamples.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkBoxCh1);
            this.groupBox5.Controls.Add(this.checkBoxCh0);
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // checkBoxCh1
            // 
            resources.ApplyResources(this.checkBoxCh1, "checkBoxCh1");
            this.checkBoxCh1.Name = "checkBoxCh1";
            this.checkBoxCh1.UseVisualStyleBackColor = true;
            this.checkBoxCh1.CheckedChanged += new System.EventHandler(this.checkBoxCh1_CheckedChanged);
            // 
            // checkBoxCh0
            // 
            resources.ApplyResources(this.checkBoxCh0, "checkBoxCh0");
            this.checkBoxCh0.Checked = true;
            this.checkBoxCh0.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCh0.Name = "checkBoxCh0";
            this.checkBoxCh0.UseVisualStyleBackColor = true;
            this.checkBoxCh0.CheckedChanged += new System.EventHandler(this.checkBoxCh0_CheckedChanged);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.label3);
            this.groupBox6.Controls.Add(this.numericUpDownSubSampleOffset);
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // numericUpDownSubSampleOffset
            // 
            this.numericUpDownSubSampleOffset.DecimalPlaces = 2;
            this.numericUpDownSubSampleOffset.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            resources.ApplyResources(this.numericUpDownSubSampleOffset, "numericUpDownSubSampleOffset");
            this.numericUpDownSubSampleOffset.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSubSampleOffset.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numericUpDownSubSampleOffset.Name = "numericUpDownSubSampleOffset";
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.textBoxConsole);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericStartPos)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericOutputSamples)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSubSampleOffset)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonBrowseReadWavFile;
        private System.Windows.Forms.TextBox textBoxReadWavFile;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonBrowseWriteFile;
        private System.Windows.Forms.TextBox textBoxWriteFile;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericStartPos;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.NumericUpDown numericOutputSamples;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBoxCh1;
        private System.Windows.Forms.CheckBox checkBoxCh0;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDownSubSampleOffset;
    }
}

