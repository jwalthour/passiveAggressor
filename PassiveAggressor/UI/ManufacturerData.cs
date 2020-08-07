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
        /// Name of the file to load - must be set to build as an embedded resource
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
            //var files = assembly.GetManifestResourceNames();
            using (Stream ouiStream = assembly.GetManifestResourceStream(OUI_RESOURCE_PATH))
            {
                using (TextFieldParser parser = new TextFieldParser(ouiStream))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int i = 0;
                    while (!parser.EndOfData && i++ < 5)
                    {
                        //Process row
                        string[] fields = parser.ReadFields();
                        foreach (string field in fields)
                        {
                            Console.Write(" " + field + ", ");
                        }
                        Console.WriteLine("");
                    }
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
                return "Unknown manufacturer";
            }
            else
            {
                return "Loading manufacturer data...";
            }
        }
    }
}
