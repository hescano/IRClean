using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IRClean
{
    public partial class Private : Form
    {
        IRCleanProtocol.IRC Connection;

        //make it available outside
        public ExRichTextBox rtbMessage
        {
            get { return this.exRichTextBox1; }
            set { this.exRichTextBox1 = value; }
        }

        public Private(IRCleanProtocol.IRC conn)
        {
            Connection = conn;
            InitializeComponent();
        }

        private void Private_FormClosing(object sender, FormClosingEventArgs e)
        {
            //we wanna keep it hidden, in case the conversation continues...
            e.Cancel = true;
            this.Hide();
        }

        private void Private_Load(object sender, EventArgs e)
        {
            //check the tag property which holds the nickname of the user
            if (this.Tag != null)
            {
                this.Text = "Private with " + this.Tag.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Connection != null)
            {
                Connection.sendMessage(this.Tag.ToString(), this.textBox1.Text);
                rtbMessage.ShowMessage(Connection.Nickname, this.textBox1.Text);
                this.textBox1.Text = "";
            }
        }
    }
}
