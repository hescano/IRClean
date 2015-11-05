
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;


namespace IRCleanProtocol
{
    /// <summary>
    /// This class controls the IRC protocol. It depends on the IRCCommand class to parse the commands.
    /// </summary>
    public class IRC
    {
        //private members
        private string previousCommand;
        private StreamReader sr;
        private StreamWriter sw;

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
            this.Server = server;
            this.Port = port;
            this.Nickname = nickname;
            this.Channel = channel;
        }

        //Events
        public event EventHandler Connected;
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<UserListEventArgs> UserList;
        public event EventHandler<String> UserJoined;
        public event EventHandler<String> UserQuit;
        public event EventHandler<NicknameChangedEventArgs> NicknameChanged;
        public event EventHandler<String> ServerMessage;
        public event EventHandler Quit;

        //Events implementation

        /// <summary>
        /// Triggers when the user joins the channel.
        /// </summary>
        protected virtual void OnConnected()
        {
            EventHandler handler = Connected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Triggers when we want to send some kind of message to the user.
        /// </summary>
        /// <param name="e">The message received, which includes the type of data.</param>
        protected virtual void OnReceived(ReceivedEventArgs e)
        {
            EventHandler<ReceivedEventArgs> handler = Received;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Triggers when the users list is received, continued, or ends
        /// </summary>
        /// <param name="e">The arguments with the list of users</param>
        protected virtual void OnUserList(UserListEventArgs e)
        {
            EventHandler<UserListEventArgs> handler = UserList;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Triggers when an user has entered the channel.
        /// </summary>
        /// <param name="userJoined">Nickname of the user who joined</param>
        protected virtual void OnUserJoined(String userJoined)
        {
            EventHandler<String> handler = UserJoined;
            if (handler != null)
            {
                handler(this, userJoined);
            }
        }

        /// <summary>
        /// Triggers when an user parts or quits the channel
        /// </summary>
        /// <param name="userQuit">Nickname of the user who left</param>
        protected virtual void OnUserQuit(String userQuit)
        {
            EventHandler<String> handler = UserQuit;
            if (handler != null)
            {
                handler(this, userQuit);
            }
        }

        /// <summary>
        /// Triggers when the user has left the chat.
        /// </summary>
        protected virtual void OnQuit()
        {
            EventHandler handler = Quit;
            if (handler != null)
            {
                handler(this, null);
            }
        }

        /// <summary>
        /// Triggers when an user has changed his/her nickname, or the server assigns a nickname
        /// </summary>
        /// <param name="oldNickname">What the nickname used to be</param>
        /// <param name="newNickname">The new nickname being used</param>
        /// <param name="type">Defines if the user changed his/her name, or if the server assigned a nickname</param>
        protected virtual void OnNicknameChanged(String oldNickname, String newNickname, NicknameChangedType type)
        {
            EventHandler<NicknameChangedEventArgs> handler = NicknameChanged;
            if (handler != null)
            {
                handler(this, new NicknameChangedEventArgs() { NewNickname = newNickname, OldNickname = oldNickname, Type = type });
            }
        }

        /// <summary>
        /// Triggers when we want to let the UI know of a displayable message
        /// </summary>
        /// <param name="message">Message to be displayed</param>
        protected virtual void OnServerMessage(String message)
        {
            EventHandler<String> handler = ServerMessage;
            if (handler != null)
            {
                handler(this, message);
            }
        }

        //Protocol Implementation

        /// <summary>
        /// Connects to an IRC chat and a channel
        /// </summary>
        public void Connect()
        {
            TcpClient client = null;
            try
            {
                //Creates a TcpClient object to connect to a IRC server
                //handle the connection async, so that the socket does not 
                //block the application
                client = new TcpClient();
                client.BeginConnect(this.Server, this.Port, new AsyncCallback(connectCallback), client);
            }
            catch
            {
                throw;
            }
        }

        private void connectCallback(IAsyncResult result)
        {
            TcpClient client = null;
            try
            {
                client = ((TcpClient)result.AsyncState);
                Stream s = client.GetStream();

                sr = new StreamReader(s,  System.Text.Encoding.Default, true);
                sw = new StreamWriter(s);
                sw.AutoFlush = true;

                //if we get here, we have a successful TCP connection
                //send first command, to establish an IRC connection
                sendNick();
                sendUser();

                while (true)
                {
                    readProtocol();
                }
            }
            catch (Exception ex)
            {
                Close();
                OnServerMessage(ex.Message);
            }
            finally
            {
                if (client != null)
                    client.Close();
            }
        }

        private void sendUser()
        {
            sendCommand("USER " + Nickname + " 8 * : a clean IRC Client.");
        }

        private void sendNick()
        {
            sendCommand("NICK " + Nickname);
        }

        public void Close()
        {
            sendCommand("QUIT");
        }

        /// <summary>
        /// IRC protocol implementation per IRC RFC 2812 https://tools.ietf.org/html/rfc2812
        /// </summary>
        public void readProtocol()
        {
            string command = sr.ReadLine();
            Console.WriteLine(command);

            IRCCommand cmd = IRCCommand.parse(command);
            if (command != null && cmd != null)
            {
                switch (cmd.Command)
                {
                    case "001": //RPL_WELCOME
                    case "002": //RPL_YOURHOST
                    case "003": //RPL_CREATED
                    case "004": //RPL_MYINFO
                    case "NOTICE": //NOTICE
                        //OnReceived(new ReceivedEventArgs() { Message = cmd.ToString(), Type = MessageType.MESSAGE_FROM_SERVER });
                        OnServerMessage(command);
                        sendCommand("JOIN " + this.Channel);
                        break;
                    case "332":
                        OnServerMessage(command);
                        break;
                    case "353": //RPL_NAMREPLY
                        //make sure if for this channel,
                        //i think i might get lists for all
                        //channels in the server.
                        if (cmd.Arguments[2] == this.Channel)
                        {
                            //lists of users for the channel we are connected
                            //lets ignore the type of channels for now (@, =, *)
                            UserListEventArgs ulea = new UserListEventArgs();

                            ulea.Users = new List<String>(cmd.Arguments[3].Split(' '));

                            //checks if the command before this one was a 353,
                            //if it is, that means the list of users was not completely
                            //received, so this packet is a continuation of the list.
                            //otherwise, this is the beginning of the list.

                            if (previousCommand != "353")
                            {
                                ulea.Type = UserListMessageType.LIST_START;
                            }
                            else if (previousCommand == "353")
                            {
                                ulea.Type = UserListMessageType.LIST_CONTINUE;
                            }

                            OnUserList(ulea);
                        }
                        break;
                    case "366": //RPL_ENDOFNAMES
                        //this is the end of the list of users.
                        OnUserList(new UserListEventArgs() { Type = UserListMessageType.LIST_END });
                        break;
                    case "433": //ERR_NICKNAMEINUSE
                        //tries to log in with a different nickname
                        sendCommand("NICK " + this.Nickname + "_" + new Random().Next(100));
                        break;
                    case "PRIVMSG": //new message to the channel, or private
                        ReceivedEventArgs rec = new ReceivedEventArgs();
                        rec.MessageFrom = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!"));

                        //TODO: handle messages that are for specific users, and show them in a unique way
                        //maybe something like FromUser > ToUser: Hello, World!

                        //lets make it easy by just worrying about
                        //public and private messages.
                        if (cmd.Arguments[0] == this.Nickname)
                        {
                            rec.Type = MessageType.MESSAGE_TO_ME;
                        }
                        else
                        {
                            rec.Type = MessageType.MESSAGE_TO_CHANNEL;
                        }

                        rec.Message = cmd.Arguments[1];
                        OnReceived(rec);
                        break;
                    case "PING": //reply with pong :p
                        sendCommand("PONG " + command.Substring(5));
                        break;
                    case "JOIN": //someone joined the channel
                        //I have two options, I could either re-ask for the list
                        //or I can just add this specific user
                        //if the channel has thousand of users, asking for the entire list
                        //could be a pain... so lets just get this user
                        string userJoined = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!"));
                        if (userJoined != this.Nickname)
                        {
                            OnUserJoined(userJoined);
                        }
                        else
                        {
                            //this is when I joined the channel.
                            //lets just say this is when we connected
                            OnConnected();
                            IsConnected = true;
                        }
                        break;
                    case "PART": //someone has left/quit the channel
                    case "QUIT":
                        string userLeft = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!"));
                        if (userLeft != this.Nickname)
                        {
                            OnUserQuit(userLeft);
                        }
                        else
                        { 
                            //left chatroom gracefully
                            OnQuit();
                            IsConnected = false;
                        }
                        break;
                    case "NICK": //change in nick
                        string newNickname;
                        string oldNickname;

                        if (cmd.Arguments != null && cmd.Arguments.Count > 0)
                        {
                            newNickname = cmd.Arguments[0];
                            oldNickname = cmd.Prefix.Substring(0, cmd.Prefix.IndexOf("!"));

                            //raise this event to update the nickname in the lists
                            OnNicknameChanged(oldNickname, newNickname, NicknameChangedType.NICK_CHANGED);
                        }

                        break;
                    case "ERROR":
                        OnQuit();
                        IsConnected = false;
                        break;
                }

                previousCommand = cmd.Command;

            }
        }

        //Commands sent to the server

        /// <summary>
        /// Sends a request for the users in the channel
        /// </summary>
        public void refreshUsers()
        {
            sendCommand("NAMES " + Channel);
        }

        /// <summary>
        /// Sends a message to the channel, or to a user.
        /// </summary>
        /// <param name="messageTo">Nicknaame of the person or channel to send the message to.</param>
        /// <param name="message">Actual text message to be sent.</param>
        public void sendMessage(string messageTo, string message)
        {
            sendCommand("PRIVMSG " + messageTo + " :" + message);
        }

        /// <summary>
        /// Sends a command to the server.
        /// </summary>
        /// <param name="command">Actual command being sent.</param>
        private void sendCommand(string command)
        {
            sw.Write(command + "\r\n");

            //flush the stream writer
            sw.Flush();
        }

    }
}
