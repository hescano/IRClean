using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IRClean
{
    public static class RTFHelper
    {
        public static void ShowMessage(String message, ExRichTextBox richTextControl, RtfColor foreColor, RtfColor backGround, FontStyle textStyle, HorizontalAlignment textAlignment = HorizontalAlignment.Left, Single textSize = 10, String textFont = "Tahoma", int crlf = 1, Control setFocusTo = null, bool isMessage = true)
        {
            if (richTextControl != null && !richTextControl.IsDisposed)
            {

                //cleaning left-over RTF characters (avoid breaking RTB)
                if (message.IndexOf(@"\") > -1)
                {
                    message = message.Replace(@"\", @"\\");
                }

                if (message.IndexOf("{") > -1)
                {
                    message = message.Replace("{", @"\{");
                }

                if (message.IndexOf("}") > -1)
                {
                    message = message.Replace("}", @"\}");
                }

                if (message.IndexOf(Environment.NewLine) > -1)
                {
                    message = message.Replace(Environment.NewLine, Environment.NewLine + @"\par  ");
                }

                //we deal with the emoticons only if it is a chat message (ignore emoticons on system message)
                if (isMessage)
                {
                    foreach (var icon in EmoticonsHelper.GetEmoticons())
                    {
                        if (icon.Key.IndexOf(",") > -1)
                        {
                            var optionalToken = icon.Key.Split(',');

                            foreach (var opt in optionalToken)
                            {
                                if (message.IndexOf(opt) > -1)
                                {
                                    //replace the token with its RTF equivalent ;)
                                    message = message.Replace(opt, icon.Value);
                                }
                            }
                        }
                        else if (message.IndexOf(icon.Key) > -1)
                        {
                            message = message.Replace(icon.Key, icon.Value);
                        }
                    }
                }

                FontStyle extraStyles = new FontStyle();
                if (message.IndexOf("") > -1)
                {
                    extraStyles |= FontStyle.Bold;
                }

                Font extraFonts = new Font(textFont, textSize, textStyle | extraStyles, GraphicsUnit.Point);

                //add extra carriage returns
                if (crlf > 0)
                {
                    for (int i = 0; i < crlf; i++)
                    {
                        message += Environment.NewLine;
                    }
                }

                RTFFractionMessage messageFractions = new RTFFractionMessage(message, foreColor, backGround);
                try
                {
                    if (message.IndexOf("\u0003") > -1)
                    {
                        string[] formattedParts = message.Split(new string[] { "\u0003" }, StringSplitOptions.None);

                        for (int i = 0; i <= formattedParts.Count() - 1; i++)
                        {
                            if (formattedParts[i].Length > 1)
                            {
                                if (IntegerHelper.IsNumeric(formattedParts[i].Substring(0, 1)))
                                {
                                    messageFractions = (ReturnRTFColor(formattedParts[i]));
                                    if (i < formattedParts.Length - 1)
                                        richTextControl.AppendTextAsRtf(messageFractions.Message, extraFonts, messageFractions.Forecolor, messageFractions.BackgroundColor);
                                }
                                else
                                {
                                    messageFractions = new RTFFractionMessage(formattedParts[i], RtfColor.Black, RtfColor.White);
                                    richTextControl.AppendTextAsRtf(messageFractions.Message, extraFonts, messageFractions.Forecolor, messageFractions.BackgroundColor);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //TODO: Handle
                }

                //write any extra text left
                richTextControl.AppendTextAsRtf(messageFractions.Message, extraFonts, messageFractions.Forecolor, messageFractions.BackgroundColor);

                //scroll to bottom of the RTB
                richTextControl.ScrollToCaret();
                //if something else needs to be focused, focus that
                if (setFocusTo != null)
                {
                    if (setFocusTo.CanFocus)
                    {
                        setFocusTo.Focus();
                    }
                }
            }
        }

        public static void ShowMessage(this ExRichTextBox box, string userName, string message)
        {
            ShowMessage(userName + ": ", box, RtfColor.Navy, RtfColor.White, FontStyle.Bold, crlf: 0);
            ShowMessage(message, (ExRichTextBox)box, RtfColor.Black, RtfColor.White, FontStyle.Regular, HorizontalAlignment.Left, crlf: 1);
        }

        public static void ShowSystemMessage(this RichTextBox box, string message)
        {
            ShowMessage("***" + message, (ExRichTextBox)box, RtfColor.Gray, RtfColor.White, FontStyle.Regular, textSize: 8, crlf: 1, isMessage: false);
        }
        public static RTFFractionMessage ReturnRTFColor(string Str)
        {
            string Definition = "";
            if (Str.Length == 2)
            {
                Definition = Str.Substring(0, 2);
            }
            else if (Str.Length == 4)
            {
                Definition = Str.Substring(0, 3);
            }
            else if (Str.Length >= 4)
            {
                Definition = Str.Substring(0, 5);
            }

            bool HasBackground = false;

            int Forecolor = 0;
            int BackgroundColor = 0;
            string Message = "";

            if (Definition.IndexOf(",") > -1)
            {
                HasBackground = true;

                Forecolor = Convert.ToInt32(Definition.Substring(0, Definition.IndexOf(",")));
                if (IntegerHelper.IsNumeric(Definition.Substring(Definition.IndexOf(",") + 1, 2)))
                {
                    BackgroundColor = Convert.ToInt32(Definition.Substring(Definition.IndexOf(",") + 1, 2));
                    Message = Str.Substring(Str.IndexOf(",") + 3);
                }
                else if (IntegerHelper.IsNumeric(Definition.Substring(Definition.IndexOf(",") + 1, 1)))
                {
                    BackgroundColor = Convert.ToInt32(Definition.Substring(Definition.IndexOf(",") + 1, 1));
                    Message = Str.Substring(Str.IndexOf(",") + 2);
                }
                else
                {
                    BackgroundColor = 0;
                    Message = Str.Substring(Str.IndexOf(","));
                }
                //No background
            }
            else
            {
                HasBackground = false;
                if (IntegerHelper.IsNumeric(Definition.Substring(0, 2)))
                {
                    Forecolor = Convert.ToInt32(Definition.Substring(0, 2));
                    Message = Str.Substring(2);
                }
                else
                {
                    Forecolor = Convert.ToInt32(Definition.Substring(0, 1));
                    Message = Str.Substring(1);
                }
            }
            return new RTFFractionMessage(Message, GeteXRTBColorFromInt(Forecolor), GeteXRTBColorFromInt(BackgroundColor));
        }

        public static RtfColor GeteXRTBColorFromInt(int iColor)
        {
            switch (iColor)
            {
                case 0:
                    return RtfColor.White;
                case 1:
                    return RtfColor.Black;
                case 2:
                    return RtfColor.Navy;
                case 3:
                    return RtfColor.Green;
                case 4:
                    return RtfColor.Red;
                case 5:
                    return RtfColor.Maroon;
                case 6:
                    return RtfColor.Purple;
                case 7:
                    return RtfColor.Olive;
                case 8:
                    return RtfColor.Yellow;
                case 9:
                    return RtfColor.Lime;
                case 10:
                    return RtfColor.Teal;
                case 11:
                    return RtfColor.Aqua;
                case 12:
                    return RtfColor.Blue;
                case 13:
                    return RtfColor.Fuchsia;
                case 14:
                    return RtfColor.Gray;
                case 15:
                    return RtfColor.Silver;
                default:
                    return RtfColor.Black;

            }
        }
    }
}

public class ViewOnlyRichTextBox : System.Windows.Forms.RichTextBox
{
    // constants for the message sending
    const int WM_SETFOCUS = 0x0007;
    const int WM_KILLFOCUS = 0x0008;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SETFOCUS) m.Msg = WM_KILLFOCUS;

        base.WndProc(ref m);
    }
}

public class RTFFractionMessage
{
    private RtfColor _ForeColor;
    private RtfColor _BackgroundColor;
    private string _Message;
    public RtfColor Forecolor
    {
        get { return _ForeColor; }
        set { _ForeColor = value; }
    }

    public RtfColor BackgroundColor
    {
        get { return _BackgroundColor; }
        set { _BackgroundColor = value; }
    }

    public string Message
    {
        get { return _Message; }
        set { _Message = value; }
    }

    public RTFFractionMessage() : base()
    {
    }
    public RTFFractionMessage(string Message, RtfColor Forecolor, RtfColor BackgroundColor)
    {
        _Message = Message;
        _ForeColor = Forecolor;
        _BackgroundColor = BackgroundColor;
    }
}
