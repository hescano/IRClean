using System;
using System.Collections.Generic;

namespace IRClean.Helpers
{
    /// <summary>
    /// Helper for IO stuff.
    /// </summary>
    public class FileHelper
    {
        /// <summary>
        /// Gets all of the badwords and their severity from a file.
        /// </summary>
        /// <param name="path">Location of a plain-text file with badwords and severity. These must be comma separated
        /// values. Ex: Ass,Bad.</param>
        /// <returns>Generic list of BadWord object.</returns>
        /// <example>
        /// This example shows how to use this method.
        /// <code>
        /// var words = FileHelper.GetAllBadWords(@"C:\Path\IRClean\Resources\BadWords.txt");
        /// foreach(BadWord bw in words)
        /// {
        ///     comboBox1.Items.Add(bw.Word);
        /// }
        /// </code>
        /// </example>
        public static List<BadWord> GetAllBadWords(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return null;

            List<BadWord> allWords = null;

            using (var file = System.IO.File.Open(path, System.IO.FileMode.Open))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(file);

                string buffer = string.Empty;
                string word = string.Empty;
                Severity sev;

                if (file != null)
                {
                    allWords = new List<BadWord>();
                    while (!sr.EndOfStream)
                    {
                        buffer = sr.ReadLine();
                        word = buffer.Split(',')[0];

                        //casts the words or the number into a severity
                        if (!Enum.TryParse(buffer.Split(',')[1], out sev))
                        {
                            sev = (Severity)Enum.Parse(typeof(Severity), buffer.Split(',')[1]);
                        }

                        allWords.Add(new BadWord() { Word = buffer.Split(',')[0], WordSeverity = sev });
                    }
                }
            }

            return allWords;
        }
    }

    /// <summary>
    /// Small structure for badwords and their severity.
    /// </summary>
    public struct BadWord
    {
        public string Word { get; set; }
        public Severity WordSeverity { get; set; }
    }
    
    /// <summary>
    /// Simple enumeration for the severity of badwords.
    /// </summary>
    public enum Severity
    {
        Ok,
        Bad,
        ReallyBad,
        Intolerable
    }
}
