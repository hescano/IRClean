using System;
using System.Windows.Forms;
using IRCleanProtocol;

namespace IRClean
{
    public partial class Connection : Form
    {
        public Connection()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //initializes the connection
            int port;
            bool isValidPort = int.TryParse(this.txtPort.Text, out port);
            string server = this.txtServer.Text;
            string nick = this.txtNickname.Text;

            if (isValidPort)
            {
                //pass the IRC object to the main window
                IRC irc = new IRC(server, port, nick, txtChannel.Text);
                Main main = new Main(irc);
                main.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Invalid port number.", "Error");
            }
        }
    }
}
