using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PassiveAggressor.UI
{
    class ManufacturerData
    {
        /// <summary>
        /// Name of the MA-L (nee OUI) file to load - must be set to build as an embedded resource.
        /// Download updated copies from: http://standards-oui.ieee.org/oui/oui.csv
        /// </summary>
        private const string OUI_RESOURCE_PATH = "PassiveAggressor.data.oui.csv"; 

        private static ManufacturerData _instance = null;
        /// <summary>
        /// Singleton instance of this class.
        /// Make sure to call LoadMfrData() to make this class useful.
        /// </summary>
        public static ManufacturerData instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ManufacturerData();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Singleton class; use ManufacturerData.instance instead
        /// </summary>
        private ManufacturerData()
        {

        }


        private Dictionary<uint, string> mfrNameForMacPrefix = null;

        public bool IsDataLoaded { get { return mfrNameForMacPrefix != null; } }

        /// <summary>
        /// Load OUI lookup data (a large CSV file)
        /// </summary>
        public void LoadMfrData()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Dictionary<uint, string> mfrDict = new Dictionary<uint, string>();
            //var files = assembly.GetManifestResourceNames();
            using (Stream ouiStream = assembly.GetManifestResourceStream(OUI_RESOURCE_PATH))
            {
                using (TextFieldParser parser = new TextFieldParser(ouiStream))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int i = 0;
                    string[] headerRow = parser.ReadFields();
                    const int MAC_COL_I = 1;
                    const int MFR_NAME_COL_I = 2;
                    if (headerRow[MAC_COL_I] != "Assignment" || headerRow[MFR_NAME_COL_I] != "Organization Name")
                    {
                        Console.WriteLine("MA-L (OUI) CSV file not in a format we understand");
                    }
                    else
                    {
                        while (!parser.EndOfData)
                        {
                            string[] fields = parser.ReadFields();
                            uint macPrefix = uint.Parse(fields[MAC_COL_I], System.Globalization.NumberStyles.HexNumber);
                            string mfr = fields[MFR_NAME_COL_I];
                            mfrDict[macPrefix] = mfr;
                        }
                    }

                    // perform this assignment at the end so IsDataLoaded can just check for null
                    mfrNameForMacPrefix = mfrDict;
                }
            }
            
        }

        /// <summary>
        /// If available, look up the name of the manufacturer based on the given MAC address
        /// </summary>
        /// <param name="mac"></param>
        /// <returns>Registered name of manufacturer, or human-readable string indicating failure.</returns>
        public string GetMfrNameForMac(PcapDotNet.Packets.Ethernet.MacAddress mac)
        {
            if (IsDataLoaded)
            {
                string macString = mac.ToString(); // comes out as XX:XX:XX:XX:XX:XX
                string macPrefixString = macString.Substring(0, 2) + macString.Substring(3, 2) + macString.Substring(6, 2); // First 3 bytes, minus colons
                uint macPrefix = uint.Parse(macPrefixString, System.Globalization.NumberStyles.HexNumber);
                if (mfrNameForMacPrefix.ContainsKey(macPrefix))
                {
                    return mfrNameForMacPrefix[macPrefix];
                }
                else
                {
                    return "Unknown manufacturer";
                }
            }
            else
            {
                return "Loading manufacturer data...";
            }
        }
    }
}
