using RandomChat;
using RandomChat.Abstractions;

namespace Client.WinForms
{
    public partial class ChatForm : Form
    {
        private readonly IRandomClient _client;

        public ChatForm(IRandomClient client)
        {
            InitializeComponent();
            _client = client;
            
            // On form close, disconnect from the server
            FormClosed += (_, _) =>
            {
                _client.DisconnectAsync().GetAwaiter().GetResult();
                Environment.Exit(0);
            };

            _client.OnPartnerMessage(_ =>
            {
                Invoke(SetHistoryContent);
                return Task.CompletedTask;
            });

            _client.OnOutgoingMessage(_ =>
            {
                Invoke(SetHistoryContent);
                return Task.CompletedTask;
            });

            _client.OnPartnerStartedTyping(() =>
            {
                Invoke(() => lblTyping.Visible = true);
                return Task.CompletedTask;
            });

            _client.OnPartnerStoppedTyping(() =>
            {
                Invoke(() => lblTyping.Visible = false);
                return Task.CompletedTask;
            });
        }

        private void SetHistoryContent()
        {
            txtHistory.Text = string.Join(Environment.NewLine, _client.History.Select(x =>
            {
                var sender = x.IsSender ? "You" : "Stranger";
                return $"{x.Message.Timestamp.ToShortTimeString()} | {sender}: {x.Message.Content}";
            }));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Invoke(() =>
            {
                _client.SendAsync(txtInput.Text).GetAwaiter().GetResult();
                txtInput.Text = string.Empty;
            });
        }

        private void txtInput_TextChanged(object sender, EventArgs e)
        {
            Invoke(() =>
            {
                _client.MarkAsTypingAsync().GetAwaiter().GetResult();
            });
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                Invoke(() =>
                {
                    _client.SendAsync(txtInput.Text).GetAwaiter().GetResult();
                    txtInput.Text = string.Empty;
                });
            }
        }
    }
}
