using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PassiveAggressor.Network
{
    class NicknameData
    {
        /// <summary>
        /// File path (relative to executing binary) for nicknames CSV file
        /// </summary>
        private const string NICKNAMES_FILENAME = "nicknames.csv";
        /// <summary>
        /// Column index containing MAC string
        /// </summary>
        private const int MAC_COL_I = 0;
        /// <summary>
        /// Header row entry for MAC string
        /// </summary>
        private const string MAC_HDR = "Mac Address (XX:XX:XX:XX:XX:XX)";
        /// <summary>
        /// Column index containing nickname string
        /// </summary>
        private const int NICKNAME_COL_I = 1;
        /// <summary>
        /// Header row entry for nickname string
        /// </summary>
        private const string NICK_HDR = "Host nickname string";


        /// <summary>
        /// Singleton class; use NicknameData.instance instead
        /// </summary>
        public NicknameData()
        {

        }

        /// <summary>
        /// Keys are string MAC addresses, of the form XX:XX:XX:XX:XX:XX.  Values are user-entered host nicknames.
        /// </summary>
        private Dictionary<string, string> nicknameForMacAddr = new Dictionary<string, string>();

        /// <summary>
        /// Load nicknames CSV file if present
        /// </summary>
        public void LoadNicknameData()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Dictionary<string, string> nickDict = new Dictionary<string, string>();
            string myPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            //var files = assembly.GetManifestResourceNames();
            try
            {
                using (Stream nickFileStream = new FileStream(myPath + "\\" + NICKNAMES_FILENAME, FileMode.Open))
                {
                    if (nickFileStream != null)
                    {
                        using (TextFieldParser parser = new TextFieldParser(nickFileStream))
                        {
                            parser.TextFieldType = FieldType.Delimited;
                            parser.SetDelimiters(",");
                            string[] headerRow = parser.ReadFields();
                            if (headerRow == null || headerRow[MAC_COL_I] != MAC_HDR || headerRow[NICKNAME_COL_I] != NICK_HDR)
                            {
                                Console.WriteLine("Host nickname CSV file not in a format we understand");
                            }
                            else
                            {
                                while (!parser.EndOfData)
                                {
                                    string[] fields = parser.ReadFields();
                                    string mac = fields[MAC_COL_I];
                                    string nick = fields[NICKNAME_COL_I];
                                    if (nick.Length > 0)
                                    {
                                        nickDict[mac] = nick;
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine("Couldn't open nickname file.");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("No nickname file (file not found).");
            }
            nicknameForMacAddr = nickDict;
        }

        /// <summary>
        /// Get the user-set nickname for the given MAC address, or empty string if none set.
        /// </summary>
        /// <param name="mac"></param>
        /// <returns>the user-set nickname for the given MAC address, or empty string if none set.</returns>
        public string GetNicknameForMac(PcapDotNet.Packets.Ethernet.MacAddress mac)
        {
            string macString = mac.ToString(); // comes out as XX:XX:XX:XX:XX:XX
            if (nicknameForMacAddr.ContainsKey(macString))
            {
                return nicknameForMacAddr[macString];
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Set nickname.  Set to an empty string to delete assignment.
        /// </summary>
        /// <param name="mac"></param>
        /// <param name="nickname"></param>
        public void SetNicknameForMac(PcapDotNet.Packets.Ethernet.MacAddress mac, string nickname)
        {
            string macString = mac.ToString(); // comes out as XX:XX:XX:XX:XX:XX
            if (nickname.Length == 0)
            {
                nicknameForMacAddr.Remove(macString);
            }
            else
            {
                nicknameForMacAddr[macString] = nickname;
            }

            SaveNicknamesToFile();
        }

        /// <summary>
        /// Save off the nicknames to disk
        /// </summary>
        private void SaveNicknamesToFile()
        {
            string myPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            using (Stream nickFileStream = new FileStream(myPath + "\\" + NICKNAMES_FILENAME, FileMode.Create))
            {
                if (nickFileStream != null)
                {
                    using (StreamWriter writer = new StreamWriter(nickFileStream))
                    {
                        writer.WriteLine(MAC_HDR + "," + NICK_HDR);
                        foreach (KeyValuePair<string, string> nickEntry in nicknameForMacAddr)
                        {
                            writer.WriteLine(nickEntry.Key + "," + nickEntry.Value);
                        }
                    }
                }
            }
        }
    }
}