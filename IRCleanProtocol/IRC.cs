
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;


namespace IRCleanProtocol
{
    /// <summary>
    /// This class controls the IRC protocol. It depends on the IRCCommand class to Parse the commands.
    /// </summary>
    public class IRC
    {
        //private members
        private string _previousCommand;
        private StreamReader _sr;
        private StreamWriter _sw;

        //Properties
        public string Server { get; set; }
        public int Port { get; set; }
        public string Nickname { get; set; }
        public string Channel { get; set; }
        //we could make it complex with different status
        //but right now only true/false should doo
        public bool IsConnected { get; set; }

        //Constructors
        public IRC(string server, int port, string nickname, string channel)
        {
            Server = server;
            Port = port;
            Nickname = nickname;
            Channel = channel;
        }

        //Events
        public event EventHandler Connected;
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<UserListEventArgs> UserList;
        public event EventHandler<string> UserJoined;
        public event EventHandler<string> UserQuit;
        public event EventHandler<NicknameChangedEventArgs> NicknameChanged;
        public event EventHandler<string> ServerMessage;
        public event EventHandler Quit;

        //Events implementation

        /// <summary>
        /// Triggers when the user joins the channel.
        /// </summary>
        protected virtual void OnConnected()
        {
            EventHandler handler = Connected;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Triggers when we want to send some kind of message to the user.
        /// </summary>
        /// <param name="e">The message received, which includes the type of data.</param>
        protected virtual void OnReceived(ReceivedEventArgs e)
        {
            EventHandler<ReceivedEventArgs> handler = Received;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers when the users list is received, continued, or ends
        /// </summary>
        /// <param name="e">The arguments with the list of users</param>
        protected virtual void OnUserList(UserListEventArgs e)
        {
            EventHandler<UserListEventArgs> handler = UserList;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers when an user has entered the channel.
        /// </summary>
        /// <param name="userJoined">Nickname of the user who joined</param>
        protected virtual void OnUserJoined(string userJoined)
        {
            if (userJoined == null) throw new ArgumentNullException(nameof(userJoined));
            EventHandler<string> handler = UserJoined;
            handler?.Invoke(this, userJoined);
        }

        /// <summary>
        /// Triggers when an user parts or quits the channel
        /// </summary>
        /// <param name="userQuit">Nickname of the user who left</param>
        protected virtual void OnUserQuit(string userQuit)
        {
            if (userQuit == null) throw new ArgumentNullException(nameof(userQuit));
            EventHandler<string> handler = UserQuit;
            handler?.Invoke(this, userQuit);
        }

        /// <summary>
        /// Triggers when the user has left the chat.
        /// </summary>
        protected virtual void OnQuit()
        {
            EventHandler handler = Quit;
            handler?.Invoke(this, null);
        }

        /// <summary>
        /// Triggers when an user has changed his/her nickname, or the server assigns a nickname
        /// </summary>
        /// <param name="oldNickname">What the nickname used to be</param>
        /// <param name="newNickname">The new nickname being used</param>
        /// <param name="type">Defines if the user changed his/her name, or if the server assigned a nickname</param>
        protected virtual void OnNicknameChanged(string oldNickname, string newNickname, NicknameChangedType type)
        {
            if (oldNickname == null) throw new ArgumentNullException(nameof(oldNickname), "Old nickname not provided.");
            if (newNickname == null) throw new ArgumentNullException(nameof(newNickname), "New nickname not provided.");
            if (!Enum.IsDefined(typeof(NicknameChangedType), type))
                throw new ArgumentOutOfRangeException(nameof(type));

            EventHandler<NicknameChangedEventArgs> handler = NicknameChanged;
            handler?.Invoke(this, new NicknameChangedEventArgs { NewNickname = newNickname, OldNickname = oldNickname, Type = type });
        }

        /// <summary>
        /// Triggers when we want to let the UI know of a displayable message
        /// </summary>
        /// <param name="message">Message to be displayed</param>
        protected virtual void OnServerMessage(string message)
        {
            EventHandler<string> handler = ServerMessage;
            handler?.Invoke(this, message);
        }

        //Protocol Implementation

        /// <summary>
        /// Connects to an IRC chat and a channel
        /// </summary>
        public void Connect()
        {
            //Creates a TcpClient object to connect to a IRC server
            //handle the connection async, to avoid blocking UIs
            var client = new TcpClient();
            client.BeginConnect(Server, Port, ConnectCallback, client);
        }

        /// <summary>
        /// Handles the callback of the connection.
        /// </summary>
        /// <param name="result">Async result.</param>
        private void ConnectCallback(IAsyncResult result)
        {
            TcpClient client = null;
            try
            {
                client = (TcpClient)result.AsyncState;
                Stream s = client.GetStream();

                _sr = new StreamReader(s, System.Text.Encoding.Default, true);
                _sw = new StreamWriter(s)
                {
                    AutoFlush = true
                };

                //we have a successful TCP connection
                //send first command, to establish an IRC connection
                SendNick();
                SendUser();

                while (true)
                {
                    ReadProtocol();
                }
            }
            catch (Exception ex)
            {
                Close();
                OnServerMessage(ex.Message);
            }
            finally
            {
                client?.Close();
            }
        }

        /// <summary>
        /// Sends the username to the server
        /// </summary>
        private void SendUser()
        {
            SendCommand("USER " + Nickname + " 8 * : a clean IRC Client.");
        }

        /// <summary>
        /// Sends the nickname to the server
        /// </summary>
        private void SendNick()
        {
            SendCommand("NICK " + Nickname);
        }

        public void Close()
        {
            SendCommand("QUIT");
        }

        /// <summary>
        /// IRC protocol implementation per IRC RFC 2812 https://tools.ietf.org/html/rfc2812
        /// </summary>
        public void ReadProtocol()
        {
            var command = _sr.ReadLine();
            Console.WriteLine(command);

            IRCCommand cmd = IRCCommand.Parse(command);
            if (command == null || cmd == null) return;

            switch (cmd.Command)
            {
                case "001": //RPL_WELCOME
                case "002": //RPL_YOURHOST
                case "003": //RPL_CREATED
                case "004": //RPL_MYINFO
                case "NOTICE": //NOTICE
                    OnServerMessage(command);
                    SendCommand("JOIN " + Channel);
                    break;
                case "332":
                    OnServerMessage(command);
                    break;
                case "353": //RPL_NAMREPLY
                    //make sure if for this channel,
                    //i think i might get lists for all
                    //channels in the server.
                    if (cmd.Arguments[2] == Channel)
                    {
                        //lists of users for the channel we are connected
                        //lets ignore the type of channels for now (@, =, *)
                        var ulea = new UserListEventArgs
                        {
                            Users = new List<string>(cmd.Arguments[3].Split(' '))
                        };


                        //checks if the command before this one was a 353,
                        //if it is, that means the list of users was not completely
                        //received, so this packet is a continuation of the list.
                        //otherwise, this is the beginning of the list.
                        if (_previousCommand != "353")
                        {
                            ulea.Type = UserListMessageType.ListStart;
                        }
                        else if (_previousCommand == "353")
                        {
                            ulea.Type = UserListMessageType.ListContinue;
                        }

                        OnUserList(ulea);
                    }
                    break;
                case "366": //RPL_ENDOFNAMES
                    //this is the end of the list of users.
                    OnUserList(new UserListEventArgs() { Type = UserListMessageType.ListEnd });
                    break;
                case "433": //ERR_NICKNAMEINUSE
                    //tries to log in with a different nickname
                    SendCommand("NICK " + Nickname + "_" + new Random().Next(100));
                    break;
                case "PRIVMSG": //new message to the channel, or private
                    ReceivedEventArgs rec = new ReceivedEventArgs
                    {
                        MessageFrom = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!", StringComparison.Ordinal))
                    };

                    //TODO: handle messages that are for specific users, and show them in a unique way
                    //maybe something like FromUser > ToUser: Hello, World!

                    //let us worry about
                    //public and private messages only
                    if (cmd.Arguments[0] == Nickname)
                    {
                        rec.Type = MessageType.MessageToMe;
                    }
                    else
                    {
                        rec.Type = MessageType.MessageToChannel;
                    }

                    rec.Message = cmd.Arguments[1];
                    OnReceived(rec);
                    break;
                case "PING": //reply with pong :p
                    SendCommand("PONG " + command.Substring(5));
                    break;
                case "JOIN": //someone joined the channel
                    //two options: We could either re-ask for the entire list
                    //or simply add the specific user
                    string userJoined = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!", StringComparison.Ordinal));
                    if (userJoined != Nickname)
                    {
                        OnUserJoined(userJoined);
                    }
                    else
                    {
                        //this is when I joined the channel.
                        OnConnected();
                        IsConnected = true;
                    }
                    break;
                case "PART": //someone has left/quit the channel
                case "QUIT":
                    string userLeft = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!", StringComparison.Ordinal));
                    if (userLeft != Nickname)
                    {
                        OnUserQuit(userLeft);
                    }
                    else
                    {
                        //leave chatroom gracefully
                        OnQuit();
                        IsConnected = false;
                    }
                    break;
                case "NICK": //change in nick

                    if (cmd.Arguments != null && cmd.Arguments.Count > 0)
                    {
                        var newNickname = cmd.Arguments[0];
                        var oldNickname = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!", StringComparison.Ordinal));

                        //raise this event to update the nickname in the lists
                        OnNicknameChanged(oldNickname, newNickname, NicknameChangedType.NickChanged);
                    }

                    break;
                case "ERROR":
                    OnQuit();
                    IsConnected = false;
                    break;
            }

            _previousCommand = cmd.Command;
        }

        //Commands sent to the server

        /// <summary>
        /// Sends a request for the users in the channel
        /// </summary>
        public void RefreshUsers()
        {
            SendCommand("NAMES " + Channel);
        }

        /// <summary>
        /// Sends a message to the channel, or to a user.
        /// </summary>
        /// <param name="messageTo">Nicknaame of the person or channel to send the message to.</param>
        /// <param name="message">Actual text message to be sent.</param>
        public void SendMessage(string messageTo, string message)
        {
            SendCommand("PRIVMSG " + messageTo + " :" + message);
        }

        /// <summary>
        /// Sends a command to the server.
        /// </summary>
        /// <param name="command">Actual command being sent.</param>
        private void SendCommand(string command)
        {
            _sw.Write(command + "\r\n");

            //flush the stream writer
            _sw.Flush();
        }

    }
}
