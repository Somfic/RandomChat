namespace Client.WinForms;

partial class ConnectToServerForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblPort = new Label();
        txtPort = new TextBox();
        btnConnect = new Button();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.ForeColor = SystemColors.Control;
        lblTitle.Location = new Point(24, 42);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(376, 42);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Connect to RandomChat";
        // 
        // lblPort
        // 
        lblPort.AutoSize = true;
        lblPort.ForeColor = SystemColors.Control;
        lblPort.Location = new Point(109, 118);
        lblPort.Name = "lblPort";
        lblPort.Size = new Size(35, 19);
        lblPort.TabIndex = 1;
        lblPort.Text = "port";
        // 
        // txtPort
        // 
        txtPort.Location = new Point(109, 140);
        txtPort.Name = "txtPort";
        txtPort.Size = new Size(182, 26);
        txtPort.TabIndex = 2;
        txtPort.TextAlign = HorizontalAlignment.Center;
        txtPort.TextChanged += txtPort_TextChanged;
        // 
        // btnConnect
        // 
        btnConnect.Enabled = false;
        btnConnect.Font = new Font("Segoe UI", 9.163636F, FontStyle.Regular, GraphicsUnit.Point);
        btnConnect.ForeColor = SystemColors.ActiveCaptionText;
        btnConnect.Location = new Point(41, 245);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new Size(328, 51);
        btnConnect.TabIndex = 3;
        btnConnect.Text = "START CHATTING";
        btnConnect.UseVisualStyleBackColor = true;
        btnConnect.Click += btnConnect_Click;
        // 
        // ConnectToServerForm
        // 
        AutoScaleDimensions = new SizeF(8F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.ActiveCaptionText;
        ClientSize = new Size(423, 318);
        Controls.Add(btnConnect);
        Controls.Add(txtPort);
        Controls.Add(lblPort);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Name = "ConnectToServerForm";
        Text = "Form1";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblPort;
    private TextBox txtPort;
    private Button btnConnect;
}