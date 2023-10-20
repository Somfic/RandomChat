namespace Client.WinForms
{
    partial class ChatForm
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
            txtInput = new TextBox();
            btnSend = new Button();
            txtHistory = new TextBox();
            lblTyping = new Label();
            SuspendLayout();
            // 
            // txtInput
            // 
            txtInput.Location = new Point(12, 424);
            txtInput.Name = "txtInput";
            txtInput.Size = new Size(440, 26);
            txtInput.TabIndex = 1;
            txtInput.TextChanged += txtInput_TextChanged;
            txtInput.KeyDown += txtInput_KeyDown;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(458, 424);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(86, 26);
            btnSend.TabIndex = 2;
            btnSend.Text = "SEND";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // txtHistory
            // 
            txtHistory.Location = new Point(12, 12);
            txtHistory.Multiline = true;
            txtHistory.Name = "txtHistory";
            txtHistory.ReadOnly = true;
            txtHistory.Size = new Size(532, 406);
            txtHistory.TabIndex = 3;
            // 
            // lblTyping
            // 
            lblTyping.AutoSize = true;
            lblTyping.BackColor = Color.Transparent;
            lblTyping.Font = new Font("Segoe UI", 9.163636F, FontStyle.Italic, GraphicsUnit.Point);
            lblTyping.Location = new Point(13, 398);
            lblTyping.Name = "lblTyping";
            lblTyping.Size = new Size(133, 19);
            lblTyping.TabIndex = 4;
            lblTyping.Text = "Stranger is typing ...";
            lblTyping.Visible = false;
            // 
            // ChatForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(556, 462);
            Controls.Add(lblTyping);
            Controls.Add(txtHistory);
            Controls.Add(btnSend);
            Controls.Add(txtInput);
            Name = "ChatForm";
            Text = "Chat";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox txtInput;
        private Button btnSend;
        private TextBox txtHistory;
        private Label lblTyping;
    }
}