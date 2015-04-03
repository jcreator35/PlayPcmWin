namespace WavSynchro
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
            this.buttonBrowseWavFile1 = new System.Windows.Forms.Button();
            this.textBoxWavFile1 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonBrowseWavFile2 = new System.Windows.Forms.Button();
            this.textBoxWavFile2 = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numericStartDelayTorelance = new System.Windows.Forms.NumericUpDown();
            this.buttonStart = new System.Windows.Forms.Button();
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.numericAccumulateSeconds = new System.Windows.Forms.NumericUpDown();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoAdjustVolumeDifference = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericStartDelayTorelance)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAccumulateSeconds)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.AccessibleDescription = null;
            this.groupBox1.AccessibleName = null;
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.BackgroundImage = null;
            this.groupBox1.Controls.Add(this.buttonBrowseWavFile1);
            this.groupBox1.Controls.Add(this.textBoxWavFile1);
            this.groupBox1.Font = null;
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // buttonBrowseWavFile1
            // 
            this.buttonBrowseWavFile1.AccessibleDescription = null;
            this.buttonBrowseWavFile1.AccessibleName = null;
            resources.ApplyResources(this.buttonBrowseWavFile1, "buttonBrowseWavFile1");
            this.buttonBrowseWavFile1.BackgroundImage = null;
            this.buttonBrowseWavFile1.Font = null;
            this.buttonBrowseWavFile1.Name = "buttonBrowseWavFile1";
            this.buttonBrowseWavFile1.UseVisualStyleBackColor = true;
            this.buttonBrowseWavFile1.Click += new System.EventHandler(this.buttonBrowseWavFile1_Click);
            // 
            // textBoxWavFile1
            // 
            this.textBoxWavFile1.AccessibleDescription = null;
            this.textBoxWavFile1.AccessibleName = null;
            resources.ApplyResources(this.textBoxWavFile1, "textBoxWavFile1");
            this.textBoxWavFile1.BackgroundImage = null;
            this.textBoxWavFile1.Font = null;
            this.textBoxWavFile1.Name = "textBoxWavFile1";
            // 
            // groupBox2
            // 
            this.groupBox2.AccessibleDescription = null;
            this.groupBox2.AccessibleName = null;
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.BackgroundImage = null;
            this.groupBox2.Controls.Add(this.buttonBrowseWavFile2);
            this.groupBox2.Controls.Add(this.textBoxWavFile2);
            this.groupBox2.Font = null;
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // buttonBrowseWavFile2
            // 
            this.buttonBrowseWavFile2.AccessibleDescription = null;
            this.buttonBrowseWavFile2.AccessibleName = null;
            resources.ApplyResources(this.buttonBrowseWavFile2, "buttonBrowseWavFile2");
            this.buttonBrowseWavFile2.BackgroundImage = null;
            this.buttonBrowseWavFile2.Font = null;
            this.buttonBrowseWavFile2.Name = "buttonBrowseWavFile2";
            this.buttonBrowseWavFile2.UseVisualStyleBackColor = true;
            this.buttonBrowseWavFile2.Click += new System.EventHandler(this.buttonBrowseWavFile2_Click);
            // 
            // textBoxWavFile2
            // 
            this.textBoxWavFile2.AccessibleDescription = null;
            this.textBoxWavFile2.AccessibleName = null;
            resources.ApplyResources(this.textBoxWavFile2, "textBoxWavFile2");
            this.textBoxWavFile2.BackgroundImage = null;
            this.textBoxWavFile2.Font = null;
            this.textBoxWavFile2.Name = "textBoxWavFile2";
            // 
            // groupBox3
            // 
            this.groupBox3.AccessibleDescription = null;
            this.groupBox3.AccessibleName = null;
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.BackgroundImage = null;
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.numericStartDelayTorelance);
            this.groupBox3.Font = null;
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
            // 
            // numericStartDelayTorelance
            // 
            this.numericStartDelayTorelance.AccessibleDescription = null;
            this.numericStartDelayTorelance.AccessibleName = null;
            resources.ApplyResources(this.numericStartDelayTorelance, "numericStartDelayTorelance");
            this.numericStartDelayTorelance.Font = null;
            this.numericStartDelayTorelance.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericStartDelayTorelance.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericStartDelayTorelance.Name = "numericStartDelayTorelance";
            this.numericStartDelayTorelance.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // buttonStart
            // 
            this.buttonStart.AccessibleDescription = null;
            this.buttonStart.AccessibleName = null;
            resources.ApplyResources(this.buttonStart, "buttonStart");
            this.buttonStart.BackgroundImage = null;
            this.buttonStart.Font = null;
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // textBoxConsole
            // 
            this.textBoxConsole.AccessibleDescription = null;
            this.textBoxConsole.AccessibleName = null;
            resources.ApplyResources(this.textBoxConsole, "textBoxConsole");
            this.textBoxConsole.BackgroundImage = null;
            this.textBoxConsole.Font = null;
            this.textBoxConsole.Name = "textBoxConsole";
            this.textBoxConsole.ReadOnly = true;
            // 
            // progressBar1
            // 
            this.progressBar1.AccessibleDescription = null;
            this.progressBar1.AccessibleName = null;
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.BackgroundImage = null;
            this.progressBar1.Font = null;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.AccessibleDescription = null;
            this.groupBox4.AccessibleName = null;
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.BackgroundImage = null;
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Controls.Add(this.numericAccumulateSeconds);
            this.groupBox4.Font = null;
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // label2
            // 
            this.label2.AccessibleDescription = null;
            this.label2.AccessibleName = null;
            resources.ApplyResources(this.label2, "label2");
            this.label2.Font = null;
            this.label2.Name = "label2";
            // 
            // numericAccumulateSeconds
            // 
            this.numericAccumulateSeconds.AccessibleDescription = null;
            this.numericAccumulateSeconds.AccessibleName = null;
            resources.ApplyResources(this.numericAccumulateSeconds, "numericAccumulateSeconds");
            this.numericAccumulateSeconds.DecimalPlaces = 1;
            this.numericAccumulateSeconds.Font = null;
            this.numericAccumulateSeconds.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericAccumulateSeconds.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericAccumulateSeconds.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericAccumulateSeconds.Name = "numericAccumulateSeconds";
            this.numericAccumulateSeconds.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // groupBox5
            // 
            this.groupBox5.AccessibleDescription = null;
            this.groupBox5.AccessibleName = null;
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.BackgroundImage = null;
            this.groupBox5.Controls.Add(this.checkBoxAutoAdjustVolumeDifference);
            this.groupBox5.Font = null;
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // checkBoxAutoAdjustVolumeDifference
            // 
            this.checkBoxAutoAdjustVolumeDifference.AccessibleDescription = null;
            this.checkBoxAutoAdjustVolumeDifference.AccessibleName = null;
            resources.ApplyResources(this.checkBoxAutoAdjustVolumeDifference, "checkBoxAutoAdjustVolumeDifference");
            this.checkBoxAutoAdjustVolumeDifference.BackgroundImage = null;
            this.checkBoxAutoAdjustVolumeDifference.Font = null;
            this.checkBoxAutoAdjustVolumeDifference.Name = "checkBoxAutoAdjustVolumeDifference";
            this.checkBoxAutoAdjustVolumeDifference.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBoxConsole);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = null;
            this.Icon = null;
            this.Name = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericStartDelayTorelance)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAccumulateSeconds)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonBrowseWavFile1;
        private System.Windows.Forms.TextBox textBoxWavFile1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonBrowseWavFile2;
        private System.Windows.Forms.TextBox textBoxWavFile2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericStartDelayTorelance;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericAccumulateSeconds;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBoxAutoAdjustVolumeDifference;
    }
}

