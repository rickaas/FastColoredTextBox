namespace Tester
{
    partial class EndOfLineSample
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EndOfLineSample));
            this.showLFButton = new System.Windows.Forms.Button();
            this.showCRButton = new System.Windows.Forms.Button();
            this.showCRLFButton = new System.Windows.Forms.Button();
            this.showTextButton = new System.Windows.Forms.Button();
            this.wordWrapCheckBox = new System.Windows.Forms.CheckBox();
            this.fctb = new FastColoredTextBoxNS.FastColoredTextBox();
            this.loadEmptyButton = new System.Windows.Forms.Button();
            this.loadTextButton = new System.Windows.Forms.Button();
            this.loadCRLFTextButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.fctb)).BeginInit();
            this.SuspendLayout();
            // 
            // showLFButton
            // 
            this.showLFButton.Location = new System.Drawing.Point(12, 12);
            this.showLFButton.Name = "showLFButton";
            this.showLFButton.Size = new System.Drawing.Size(75, 23);
            this.showLFButton.TabIndex = 1;
            this.showLFButton.Text = "LF";
            this.showLFButton.UseVisualStyleBackColor = true;
            this.showLFButton.Click += new System.EventHandler(this.showLFButton_Click);
            // 
            // showCRButton
            // 
            this.showCRButton.Location = new System.Drawing.Point(93, 12);
            this.showCRButton.Name = "showCRButton";
            this.showCRButton.Size = new System.Drawing.Size(75, 23);
            this.showCRButton.TabIndex = 2;
            this.showCRButton.Text = "CR";
            this.showCRButton.UseVisualStyleBackColor = true;
            this.showCRButton.Click += new System.EventHandler(this.showCRButton_Click);
            // 
            // showCRLFButton
            // 
            this.showCRLFButton.Location = new System.Drawing.Point(174, 12);
            this.showCRLFButton.Name = "showCRLFButton";
            this.showCRLFButton.Size = new System.Drawing.Size(75, 23);
            this.showCRLFButton.TabIndex = 3;
            this.showCRLFButton.Text = "CR-LF";
            this.showCRLFButton.UseVisualStyleBackColor = true;
            this.showCRLFButton.Click += new System.EventHandler(this.showCRLFButton_Click);
            // 
            // showTextButton
            // 
            this.showTextButton.Location = new System.Drawing.Point(356, 12);
            this.showTextButton.Name = "showTextButton";
            this.showTextButton.Size = new System.Drawing.Size(75, 23);
            this.showTextButton.TabIndex = 4;
            this.showTextButton.Text = "Show EOL";
            this.showTextButton.UseVisualStyleBackColor = true;
            this.showTextButton.Click += new System.EventHandler(this.showTextButton_Click);
            // 
            // wordWrapCheckBox
            // 
            this.wordWrapCheckBox.AutoSize = true;
            this.wordWrapCheckBox.Location = new System.Drawing.Point(255, 16);
            this.wordWrapCheckBox.Name = "wordWrapCheckBox";
            this.wordWrapCheckBox.Size = new System.Drawing.Size(75, 17);
            this.wordWrapCheckBox.TabIndex = 5;
            this.wordWrapCheckBox.Text = "Wordwrap";
            this.wordWrapCheckBox.UseVisualStyleBackColor = true;
            // 
            // fctb
            // 
            this.fctb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fctb.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            this.fctb.BackBrush = null;
            this.fctb.CharHeight = 14;
            this.fctb.CharWidth = 8;
            this.fctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fctb.DefaultEolFormat = FastColoredTextBoxNS.EolFormat.CRLF;
            this.fctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fctb.EndOfLineStyle = null;
            this.fctb.Hotkeys = resources.GetString("fctb.Hotkeys");
            this.fctb.IsReplaceMode = false;
            this.fctb.Location = new System.Drawing.Point(12, 85);
            this.fctb.Name = "fctb";
            this.fctb.Paddings = new System.Windows.Forms.Padding(0);
            this.fctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fctb.Size = new System.Drawing.Size(451, 292);
            this.fctb.TabIndex = 0;
            this.fctb.Zoom = 100;
            // 
            // loadEmptyButton
            // 
            this.loadEmptyButton.Location = new System.Drawing.Point(12, 42);
            this.loadEmptyButton.Name = "loadEmptyButton";
            this.loadEmptyButton.Size = new System.Drawing.Size(75, 23);
            this.loadEmptyButton.TabIndex = 6;
            this.loadEmptyButton.Text = "Empty";
            this.loadEmptyButton.UseVisualStyleBackColor = true;
            this.loadEmptyButton.Click += new System.EventHandler(this.loadEmptyButton_Click);
            // 
            // loadTextButton
            // 
            this.loadTextButton.Location = new System.Drawing.Point(93, 42);
            this.loadTextButton.Name = "loadTextButton";
            this.loadTextButton.Size = new System.Drawing.Size(75, 23);
            this.loadTextButton.TabIndex = 7;
            this.loadTextButton.Text = "LF Text";
            this.loadTextButton.UseVisualStyleBackColor = true;
            this.loadTextButton.Click += new System.EventHandler(this.loadTextButton_Click);
            // 
            // loadCRLFTextButton
            // 
            this.loadCRLFTextButton.Location = new System.Drawing.Point(174, 42);
            this.loadCRLFTextButton.Name = "loadCRLFTextButton";
            this.loadCRLFTextButton.Size = new System.Drawing.Size(75, 23);
            this.loadCRLFTextButton.TabIndex = 8;
            this.loadCRLFTextButton.Text = "CR-LF Text";
            this.loadCRLFTextButton.UseVisualStyleBackColor = true;
            this.loadCRLFTextButton.Click += new System.EventHandler(this.loadCRLFTextButton_Click);
            // 
            // EndOfLineSample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(475, 389);
            this.Controls.Add(this.loadCRLFTextButton);
            this.Controls.Add(this.loadTextButton);
            this.Controls.Add(this.loadEmptyButton);
            this.Controls.Add(this.wordWrapCheckBox);
            this.Controls.Add(this.showTextButton);
            this.Controls.Add(this.showCRLFButton);
            this.Controls.Add(this.showCRButton);
            this.Controls.Add(this.showLFButton);
            this.Controls.Add(this.fctb);
            this.Name = "EndOfLineSample";
            this.Text = "EndOfLineSample";
            ((System.ComponentModel.ISupportInitialize)(this.fctb)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FastColoredTextBoxNS.FastColoredTextBox fctb;
        private System.Windows.Forms.Button showLFButton;
        private System.Windows.Forms.Button showCRButton;
        private System.Windows.Forms.Button showCRLFButton;
        private System.Windows.Forms.Button showTextButton;
        private System.Windows.Forms.CheckBox wordWrapCheckBox;
        private System.Windows.Forms.Button loadEmptyButton;
        private System.Windows.Forms.Button loadTextButton;
        private System.Windows.Forms.Button loadCRLFTextButton;
    }
}