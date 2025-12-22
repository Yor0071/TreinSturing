namespace TreinSturing
{
    partial class ProgForm
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
            this.BackButton = new System.Windows.Forms.Button();
            this.plcGrid = new System.Windows.Forms.DataGridView();
            this.comboDb = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.plcGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // BackButton
            // 
            this.BackButton.Location = new System.Drawing.Point(901, 494);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(94, 32);
            this.BackButton.TabIndex = 0;
            this.BackButton.Text = "Terug";
            this.BackButton.UseVisualStyleBackColor = true;
            this.BackButton.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // comboDb
            // 
            this.comboDb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDb.Location = new System.Drawing.Point(12, 12);
            this.comboDb.Name = "comboDb";
            this.comboDb.Size = new System.Drawing.Size(200, 24);
            this.comboDb.TabIndex = 1;
            this.comboDb.SelectedIndexChanged += new System.EventHandler(this.comboDb_SelectedIndexChanged);
            // 
            // plcGrid
            // 
            this.plcGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.plcGrid.Location = new System.Drawing.Point(12, 49);
            this.plcGrid.Name = "plcGrid";
            this.plcGrid.RowHeadersWidth = 51;
            this.plcGrid.RowTemplate.Height = 24;
            this.plcGrid.Size = new System.Drawing.Size(983, 439);
            this.plcGrid.TabIndex = 2;
            // 
            // ProgForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1007, 538);
            this.Controls.Add(this.plcGrid);
            this.Controls.Add(this.comboDb);
            this.Controls.Add(this.BackButton);
            this.Name = "ProgForm";
            this.Text = "ProgForm";
            this.Load += new System.EventHandler(this.ProgForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.plcGrid)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button BackButton;
        private System.Windows.Forms.DataGridView plcGrid;
        private System.Windows.Forms.ComboBox comboDb;
    }
}