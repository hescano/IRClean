using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCleanProtocol
{
    /// <summary>
    /// This class' main function is to parse raw IRC commands into prefixes, commands and arguments.
    /// </summary>
    public class IRCCommand
    {
        public string Prefix { get; set; }
        public string Command { get; set; }
        public List<string> Arguments { get; set; }

        /// <summary>
        /// Get the IRC command, per IRC RFC 2812 https://tools.ietf.org/html/rfc2812
        /// </summary>
        /// <param name="raw">Raw string to be parsed.</param>
        /// <returns>An IRCCommand object with all the members to make sense out of the protocol.</returns>
        public static IRCCommand parse(string raw)
        {
            IRCCommand tmp = null;

            if (!string.IsNullOrEmpty(raw))
            {
                int prefixEnd = -1, trailingStart = raw.Length;
                string trailing = null;

                try
                {
                    if (raw.StartsWith(":"))
                    {
                        tmp = new IRCCommand();
                        prefixEnd = raw.IndexOf(" ");
                        tmp.Prefix = raw.Substring(1, prefixEnd - 1);

                        trailingStart = raw.IndexOf(" :");

                        if (trailingStart > -1)
                            trailing = raw.Substring(trailingStart + 2);
                        else
                            trailingStart = raw.Length;

                        var commandAndParameters = raw.Substring(prefixEnd + 1, trailingStart - prefixEnd - 1).Split(' ');

                        tmp.Command = commandAndParameters.First(); //this is the command

                        if (commandAndParameters.Length > 1)
                            tmp.Arguments = commandAndParameters.Skip(1).ToList(); //dont take the first one, thats the command

                        if (!String.IsNullOrEmpty(trailing))
                        {
                            if (tmp.Arguments == null)
                            {
                                tmp.Arguments = new List<string>();
                            }
                            tmp.Arguments = tmp.Arguments.Concat(new string[] { trailing }).ToList();
                        }

                    }
                    else
                    {
                        if (raw.ToLower().StartsWith("ping"))
                        {
                            //TODO: Maybe make this prettier.
                            tmp = new IRCCommand();
                            tmp.Command = "PING";
                            tmp.Arguments = new List<string>();
                            tmp.Arguments.Add(raw.Substring(5));
                        }
                        else if (raw.ToLower().StartsWith("error"))
                        {
                            tmp = new IRCCommand();
                            tmp.Command = "ERROR";
                            tmp.Arguments = new List<string>();
                            //ERROR :Closing Link: IRClean by io.chathispano.com (Quit)
                            tmp.Arguments.AddRange(raw.Split(':').Skip(1));
                        }
                    }
                }
                catch
                {
                    tmp = null;
                }
            }
            return tmp;
        }

        /// <summary>
        /// Converts a list of IRC arguments into a string.
        /// </summary>
        /// <returns>A string representing the command from the server.</returns>
        public override string ToString()
        {
            string str = "";
            for (int i = 1; i < Arguments.Count; i++)
            {
                str += Arguments[i];
            }

            return str;
        }
    }
}
