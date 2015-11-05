using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IRCleanProtocol;

namespace IRClean
{
    public partial class Main : Form
    {
        public IRC Connection;
        private List<String> users;
        public bool canLeave = false;
        List<Private> privateChats;

        public Main(IRC irc)
        {
            InitializeComponent();
            irc.Connect();
            lblServerName.Text = irc.Server;
            lblChannelName.Text = irc.Channel;


            //event handlers from the IRC object
            irc.Received += OnMessageReceived;
            irc.Connected += OnConnected;
            irc.UserList += OnUserList;
            irc.UserJoined += OnUserJoined;
            irc.UserQuit += OnUserQuit;
            irc.NicknameChanged += OnNicknameChanged;
            irc.Quit += OnQuit;
            irc.ServerMessage += OnServerMessageReceived;
            Connection = irc;
        }

        /// <summary>
        /// Triggered when the client is free to exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnQuit(object sender, EventArgs e)
        {
            canLeave = true;
            Application.Exit();
        }

        /// <summary>
        /// When an user changes his/her name.
        /// </summary>
        void OnNicknameChanged(object sender, NicknameChangedEventArgs e)
        {
            if (e.Type == NicknameChangedType.NICK_CHANGED)
            {
                //get the user in the user list for update
                int index = this.listUsers.Items.IndexOf(e.OldNickname);

                if (index > -1)
                {
                    this.UIThread(() => this.listUsers.Items[index] = e.NewNickname);
                    this.UIThread(() => this.rtbMessage.ShowSystemMessage("The user <" + e.OldNickname + "> has changed his nickname  to " + e.NewNickname + "."));
                }
            }
        }

        /// <summary>
        /// When an user leaves the chat.
        /// </summary>
        void OnUserQuit(object sender, string e)
        {
            if (this.listUsers.Items.IndexOf(e) > -1)
            {
                this.UIThread(() => this.listUsers.Items.RemoveAt(this.listUsers.Items.IndexOf(e)));
                updateUsersCount();
                this.UIThread(() => this.rtbMessage.ShowSystemMessage("The user <" + e + "> has left the channel."));
            }
        }

        /// <summary>
        /// Updates the label with users count.
        /// </summary>
        void updateUsersCount()
        {
            this.UIThread(() => this.lblTotalUsers.Text = "Total Users: " + this.listUsers.Items.Count);
        }

        /// <summary>
        /// When an user joins the channel.
        /// </summary>
        void OnUserJoined(object sender, string e)
        {
            this.UIThread(() => this.listUsers.Items.Add(e));
            this.UIThread(() => this.rtbMessage.ShowSystemMessage("The user <" + e + "> has joined the channel."));
            updateUsersCount();
        }

        /// <summary>
        /// When the list of users is received.
        /// </summary>
        void OnUserList(object sender, UserListEventArgs e)
        {
            if (e.Type == UserListMessageType.LIST_START)
            {
                //beginning of list, lets add users
                this.UIThread(() => this.listUsers.Items.Clear());
                if (users != null)
                {
                    users.Clear();
                }
                else
                {
                    users = new List<string>();
                }
            }

            if (e.Type == UserListMessageType.LIST_START || e.Type == UserListMessageType.LIST_CONTINUE)
            {
                //add users to list if the list just starts, or if it continues
                if (e.Users != null && e.Users.Count > 0)
                {
                    users.AddRange(e.Users);
                }
            }
            else if (e.Type == UserListMessageType.LIST_END)
            {
                //this adds them all at the end,
                //versus adding chunks as we get them
                foreach (String usr in users)
                {
                    this.UIThread(() => this.listUsers.Items.Add(usr));
                }

                updateUsersCount();
            }
        }

        /// <summary>
        /// When the user connects to the chat.
        /// </summary>
        private void OnConnected(object sender, EventArgs e)
        {
            this.UIThread(() => this.rtbMessage.ShowSystemMessage("Connection to chat room successful..."));
        }

        /// <summary>
        /// When an user recieves a public or private message.
        /// </summary>
        private void OnMessageReceived(object sender, ReceivedEventArgs e)
        {
            if (e != null)
            {
                if (e.Type == MessageType.MESSAGE_FROM_SERVER)
                {
                    this.UIThread(() => this.rtbMessage.ShowSystemMessage(e.Message));
                }
                else if (e.Type == MessageType.MESSAGE_TO_ME)
                {
                    Private prv = getPrivateByUser(e.MessageFrom);

                    this.UIThread(() => prv.rtbMessage.ShowMessage(e.MessageFrom, e.Message));
                    this.UIThread(() => prv.Show());
                }
                else if (e.Type == MessageType.MESSAGE_TO_CHANNEL)
                {
                    this.UIThread(() => this.rtbMessage.ShowMessage(e.MessageFrom, e.Message));
                }
            }
        }

        /// <summary>
        /// A server message was received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnServerMessageReceived(object sender, string e)
        {
            if (e != null)
            {
                this.UIThread(() => this.rtbMessage.ShowSystemMessage(e));
            }
        }
        /// <summary>
        /// Gets the private window given the username of the person chatting.
        /// </summary>
        Private getPrivateByUser(string userName)
        {
            if (privateChats == null)
                privateChats = new List<Private>();


            //lets check the tag to see if this private window 
            //already exists
            Private prv = privateChats.Where(p => p.Tag.ToString() == userName).FirstOrDefault();
            if (prv == null)
            {
                //private chat did not exist, create it
                prv = new Private(this.Connection);
                prv.Tag = userName;
                privateChats.Add(prv); //make it available for future conversations
            }

            return prv;
        }

        //form events
        private void Main_Load(object sender, EventArgs e)
        {
            rtbMessage.ShowSystemMessage("Welcome to IRClean 0.1 Beta. Created by Hanlet Escaño, and Roberto Galindo. http://www.softwarequest.net");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "")
            {
                Connection.sendMessage(Connection.Channel, txtMessage.Text);
                rtbMessage.ShowMessage(Connection.Nickname, txtMessage.Text);
                txtMessage.Text = "";
            }
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                e.Handled = true;
                btnSend_Click(null, null);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.listUsers.Items.Clear();
            Connection.refreshUsers();
        }

        private void rtbMessage_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to navigate to that URL?", "Choose one", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
        }

        private void listUsers_DoubleClick(object sender, EventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb != null)
            {
                String usr = lb.SelectedItem.ToString();

                if (usr != Connection.Nickname && usr != "")
                {
                    Private prv = getPrivateByUser(usr);
                    if (prv != null)
                    {
                        this.UIThread(() => prv.Show());
                    }
                }
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (canLeave)
            {
                e.Cancel = false;
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to close this window?", "Close?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    Connection.Close();
                    e.Cancel = true;
                }
            }
        }
    }
}
