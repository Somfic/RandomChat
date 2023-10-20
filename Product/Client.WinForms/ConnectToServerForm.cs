using System.Net;
using RandomChat;
using RandomChat.Abstractions;

namespace Client.WinForms;

public partial class ConnectToServerForm : Form
{
    private readonly IRandomClient _client;

    public ConnectToServerForm(IRandomClient client)
    {
        _client = client;

        var chat = new ChatForm(client);

        _client.OnPartnerConnected(() =>
        {
            Invoke(() =>
            {
                chat.Show();
                Hide();
            });
            return Task.CompletedTask;
        });

        _client.OnPartnerDisconnected(() =>
        {
            Invoke(() =>
            {
                chat.Hide();
                Show();
            });
            return Task.CompletedTask;
        });

        InitializeComponent();
    }

    private void txtPort_TextChanged(object sender, EventArgs e)
    {
        if (int.TryParse(txtPort.Text, out var port))
            btnConnect.Enabled = port is >= IPEndPoint.MinPort and <= IPEndPoint.MaxPort;
        else
            btnConnect.Enabled = false;
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        Task.Run(() =>
        {
            _client.ConnectAsync(int.Parse(txtPort.Text)).GetAwaiter().GetResult();
            txtPort.Invoke(() => txtPort.Visible = false);
            btnConnect.Invoke(() => btnConnect.Visible = false);
            lblPort.Invoke(() => lblPort.Visible = false);
            lblTitle.Invoke(() => lblTitle.Text = "Waiting for stranger...");
        });
    }
}