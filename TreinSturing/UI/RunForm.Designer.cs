namespace TreinSturing
{
    partial class RunForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ProgButton = new System.Windows.Forms.Button();
            this.RunButton = new System.Windows.Forms.Button();
            this.StopButton = new System.Windows.Forms.Button();
            this.textLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ProgButton
            // 
            this.ProgButton.Location = new System.Drawing.Point(12, 12);
            this.ProgButton.Name = "ProgButton";
            this.ProgButton.Size = new System.Drawing.Size(128, 31);
            this.ProgButton.TabIndex = 0;
            this.ProgButton.Text = "Programmeren";
            this.ProgButton.UseVisualStyleBackColor = true;
            this.ProgButton.Click += new System.EventHandler(this.ProgButton_Click);
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(146, 12);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(109, 31);
            this.RunButton.TabIndex = 1;
            this.RunButton.Text = "Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // StopButton
            // 
            this.StopButton.Location = new System.Drawing.Point(261, 12);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(101, 30);
            this.StopButton.TabIndex = 2;
            this.StopButton.Text = "Stop";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // textLog
            // 
            this.textLog = new System.Windows.Forms.TextBox();
            this.textLog.Multiline = true;
            this.textLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textLog.Location = new System.Drawing.Point(12, 60);
            this.textLog.Size = new System.Drawing.Size(500, 700);
            this.textLog.Name = "textLog";
            this.textLog.ReadOnly = true;
            this.Controls.Add(this.textLog);
            // 
            // RunForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1593, 793);
            this.Controls.Add(this.textLog);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.ProgButton);
            this.Name = "RunForm";
            this.Text = "RunForm";
            this.Load += new System.EventHandler(this.RunForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ProgButton;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Button StopButton;
        private System.Windows.Forms.TextBox textLog;
    }
}