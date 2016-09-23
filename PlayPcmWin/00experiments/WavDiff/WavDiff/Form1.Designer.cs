namespace WavDiff
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
            this.buttonStart = new System.Windows.Forms.Button();
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonRead1 = new System.Windows.Forms.Button();
            this.textBoxRead1 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonRead2 = new System.Windows.Forms.Button();
            this.textBoxRead2 = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.labelMagnitude = new System.Windows.Forms.Label();
            this.numericMagnitude = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.buttonWrite = new System.Windows.Forms.Button();
            this.textBoxWrite = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoAdjustVolumeDifference = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numericToleranceSeconds = new System.Windows.Forms.NumericUpDown();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.labelAccumulateSeconds = new System.Windows.Forms.Label();
            this.numericAccumulateSeconds = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericMagnitude)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericToleranceSeconds)).BeginInit();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAccumulateSeconds)).BeginInit();
            this.SuspendLayout();
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
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
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
            // groupBox1
            // 
            this.groupBox1.AccessibleDescription = null;
            this.groupBox1.AccessibleName = null;
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.BackgroundImage = null;
            this.groupBox1.Controls.Add(this.buttonRead1);
            this.groupBox1.Controls.Add(this.textBoxRead1);
            this.groupBox1.Font = null;
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // buttonRead1
            // 
            this.buttonRead1.AccessibleDescription = null;
            this.buttonRead1.AccessibleName = null;
            resources.ApplyResources(this.buttonRead1, "buttonRead1");
            this.buttonRead1.BackgroundImage = null;
            this.buttonRead1.Font = null;
            this.buttonRead1.Name = "buttonRead1";
            this.buttonRead1.UseVisualStyleBackColor = true;
            this.buttonRead1.Click += new System.EventHandler(this.buttonRead1_Click);
            // 
            // textBoxRead1
            // 
            this.textBoxRead1.AccessibleDescription = null;
            this.textBoxRead1.AccessibleName = null;
            resources.ApplyResources(this.textBoxRead1, "textBoxRead1");
            this.textBoxRead1.BackgroundImage = null;
            this.textBoxRead1.Font = null;
            this.textBoxRead1.Name = "textBoxRead1";
            // 
            // groupBox2
            // 
            this.groupBox2.AccessibleDescription = null;
            this.groupBox2.AccessibleName = null;
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.BackgroundImage = null;
            this.groupBox2.Controls.Add(this.buttonRead2);
            this.groupBox2.Controls.Add(this.textBoxRead2);
            this.groupBox2.Font = null;
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // buttonRead2
            // 
            this.buttonRead2.AccessibleDescription = null;
            this.buttonRead2.AccessibleName = null;
            resources.ApplyResources(this.buttonRead2, "buttonRead2");
            this.buttonRead2.BackgroundImage = null;
            this.buttonRead2.Font = null;
            this.buttonRead2.Name = "buttonRead2";
            this.buttonRead2.UseVisualStyleBackColor = true;
            this.buttonRead2.Click += new System.EventHandler(this.buttonRead2_Click);
            // 
            // textBoxRead2
            // 
            this.textBoxRead2.AccessibleDescription = null;
            this.textBoxRead2.AccessibleName = null;
            resources.ApplyResources(this.textBoxRead2, "textBoxRead2");
            this.textBoxRead2.BackgroundImage = null;
            this.textBoxRead2.Font = null;
            this.textBoxRead2.Name = "textBoxRead2";
            // 
            // groupBox3
            // 
            this.groupBox3.AccessibleDescription = null;
            this.groupBox3.AccessibleName = null;
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.BackgroundImage = null;
            this.groupBox3.Controls.Add(this.labelMagnitude);
            this.groupBox3.Controls.Add(this.numericMagnitude);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Font = null;
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // labelMagnitude
            // 
            this.labelMagnitude.AccessibleDescription = null;
            this.labelMagnitude.AccessibleName = null;
            resources.ApplyResources(this.labelMagnitude, "labelMagnitude");
            this.labelMagnitude.Font = null;
            this.labelMagnitude.Name = "labelMagnitude";
            // 
            // numericMagnitude
            // 
            this.numericMagnitude.AccessibleDescription = null;
            this.numericMagnitude.AccessibleName = null;
            resources.ApplyResources(this.numericMagnitude, "numericMagnitude");
            this.numericMagnitude.DecimalPlaces = 1;
            this.numericMagnitude.Font = null;
            this.numericMagnitude.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericMagnitude.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericMagnitude.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericMagnitude.Name = "numericMagnitude";
            this.numericMagnitude.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericMagnitude.ValueChanged += new System.EventHandler(this.numericMagnitude_ValueChanged);
            this.numericMagnitude.KeyUp += new System.Windows.Forms.KeyEventHandler(this.numericMagnitude_KeyUp);
            this.numericMagnitude.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericMagnitude_KeyPress);
            this.numericMagnitude.KeyDown += new System.Windows.Forms.KeyEventHandler(this.numericMagnitude_KeyDown);
            // 
            // label3
            // 
            this.label3.AccessibleDescription = null;
            this.label3.AccessibleName = null;
            resources.ApplyResources(this.label3, "label3");
            this.label3.Font = null;
            this.label3.Name = "label3";
            // 
            // groupBox4
            // 
            this.groupBox4.AccessibleDescription = null;
            this.groupBox4.AccessibleName = null;
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.BackgroundImage = null;
            this.groupBox4.Controls.Add(this.buttonWrite);
            this.groupBox4.Controls.Add(this.textBoxWrite);
            this.groupBox4.Font = null;
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // buttonWrite
            // 
            this.buttonWrite.AccessibleDescription = null;
            this.buttonWrite.AccessibleName = null;
            resources.ApplyResources(this.buttonWrite, "buttonWrite");
            this.buttonWrite.BackgroundImage = null;
            this.buttonWrite.Font = null;
            this.buttonWrite.Name = "buttonWrite";
            this.buttonWrite.UseVisualStyleBackColor = true;
            this.buttonWrite.Click += new System.EventHandler(this.buttonWrite_Click);
            // 
            // textBoxWrite
            // 
            this.textBoxWrite.AccessibleDescription = null;
            this.textBoxWrite.AccessibleName = null;
            resources.ApplyResources(this.textBoxWrite, "textBoxWrite");
            this.textBoxWrite.BackgroundImage = null;
            this.textBoxWrite.Font = null;
            this.textBoxWrite.Name = "textBoxWrite";
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
            // groupBox6
            // 
            this.groupBox6.AccessibleDescription = null;
            this.groupBox6.AccessibleName = null;
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.BackgroundImage = null;
            this.groupBox6.Controls.Add(this.label1);
            this.groupBox6.Controls.Add(this.numericToleranceSeconds);
            this.groupBox6.Font = null;
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
            // 
            // numericToleranceSeconds
            // 
            this.numericToleranceSeconds.AccessibleDescription = null;
            this.numericToleranceSeconds.AccessibleName = null;
            resources.ApplyResources(this.numericToleranceSeconds, "numericToleranceSeconds");
            this.numericToleranceSeconds.Font = null;
            this.numericToleranceSeconds.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericToleranceSeconds.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericToleranceSeconds.Name = "numericToleranceSeconds";
            this.numericToleranceSeconds.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // groupBox7
            // 
            this.groupBox7.AccessibleDescription = null;
            this.groupBox7.AccessibleName = null;
            resources.ApplyResources(this.groupBox7, "groupBox7");
            this.groupBox7.BackgroundImage = null;
            this.groupBox7.Controls.Add(this.labelAccumulateSeconds);
            this.groupBox7.Controls.Add(this.numericAccumulateSeconds);
            this.groupBox7.Controls.Add(this.label5);
            this.groupBox7.Font = null;
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.TabStop = false;
            // 
            // labelAccumulateSeconds
            // 
            this.labelAccumulateSeconds.AccessibleDescription = null;
            this.labelAccumulateSeconds.AccessibleName = null;
            resources.ApplyResources(this.labelAccumulateSeconds, "labelAccumulateSeconds");
            this.labelAccumulateSeconds.Font = null;
            this.labelAccumulateSeconds.Name = "labelAccumulateSeconds";
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
            this.numericAccumulateSeconds.ValueChanged += new System.EventHandler(this.numericAccumulateSeconds_ValueChanged);
            this.numericAccumulateSeconds.KeyUp += new System.Windows.Forms.KeyEventHandler(this.numericAccumulateSeconds_KeyUp);
            this.numericAccumulateSeconds.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericAccumulateSeconds_KeyPress);
            this.numericAccumulateSeconds.KeyDown += new System.Windows.Forms.KeyEventHandler(this.numericAccumulateSeconds_KeyDown);
            // 
            // label5
            // 
            this.label5.AccessibleDescription = null;
            this.label5.AccessibleName = null;
            resources.ApplyResources(this.label5, "label5");
            this.label5.Font = null;
            this.label5.Name = "label5";
            // 
            // Form1
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBoxConsole);
            this.Controls.Add(this.buttonStart);
            this.Font = null;
            this.Icon = null;
            this.Name = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericMagnitude)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericToleranceSeconds)).EndInit();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericAccumulateSeconds)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonRead1;
        private System.Windows.Forms.TextBox textBoxRead1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonRead2;
        private System.Windows.Forms.TextBox textBoxRead2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label labelMagnitude;
        private System.Windows.Forms.NumericUpDown numericMagnitude;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button buttonWrite;
        private System.Windows.Forms.TextBox textBoxWrite;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBoxAutoAdjustVolumeDifference;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericToleranceSeconds;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.NumericUpDown numericAccumulateSeconds;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelAccumulateSeconds;
    }
}

