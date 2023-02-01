
namespace TrovoStreamChecker
{
    partial class Form1
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
            this.dataGridOnline = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridOnline)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridOnline
            // 
            this.dataGridOnline.AllowUserToAddRows = false;
            this.dataGridOnline.AllowUserToDeleteRows = false;
            this.dataGridOnline.AllowUserToResizeRows = false;
            this.dataGridOnline.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridOnline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridOnline.Location = new System.Drawing.Point(0, 0);
            this.dataGridOnline.MultiSelect = false;
            this.dataGridOnline.Name = "dataGridOnline";
            this.dataGridOnline.ReadOnly = true;
            this.dataGridOnline.RowHeadersVisible = false;
            this.dataGridOnline.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridOnline.ShowCellErrors = false;
            this.dataGridOnline.ShowCellToolTips = false;
            this.dataGridOnline.ShowEditingIcon = false;
            this.dataGridOnline.ShowRowErrors = false;
            this.dataGridOnline.Size = new System.Drawing.Size(533, 555);
            this.dataGridOnline.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 555);
            this.Controls.Add(this.dataGridOnline);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "Form1";
            this.Text = "Стримеры онлайн";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridOnline)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridOnline;
    }
}

