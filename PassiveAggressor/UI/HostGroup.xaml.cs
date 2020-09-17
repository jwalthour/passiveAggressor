using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PassiveAggressor.UI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class HostGroup : UserControl
    {
        public string MfrDesc { get; private set; }
        private Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> hosts;
        private Dictionary<ulong, VisibleHost> hostControlsAdded = new Dictionary<ulong, VisibleHost>();

        /// <summary>
        /// WIll be called when the user expands this group
        /// </summary>
        /// <param name="justClicked"></param>
        public delegate void UserExpandedHostGroup_d(HostGroup justClicked);
        public UserExpandedHostGroup_d UserExpandedHost = null;

        public HostGroup(string mfrDesc, string iconResourceName)
        {
            InitializeComponent();

            MfrDesc = mfrDesc;
            imageMfrIcon.Source = LoadImage(iconResourceName);
        }

        public void UpdateVisibleHostsList(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> Hosts)
        {
            hosts = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>(Hosts);
            labelMfrString.Content = MfrDesc + "\n(" + Hosts.Count.ToString() + " hosts)";

            if (expanderHostsList.IsExpanded)
            {
                PopulateHostList();
            }
        }

        public void CollapseHostList()
        {
            expanderHostsList.IsExpanded = false;
        }

        /// <summary>
        /// Called whenever an event informs us the user has updated (edited, added, or deleted) a host nickname
        /// </summary>
        private void SortHostList(UI.VisibleHost hostControlUpdated)
        {
            // Re-sort the hosts list
            List<UI.VisibleHost> sortedHosts = new List<UI.VisibleHost>();
            foreach (UI.VisibleHost existingHost in stackHostList.Children)
            {
                sortedHosts.Add(existingHost);
            }
            sortedHosts.Sort(CompareHostsForList);
            stackHostList.Children.Clear();
            foreach (UI.VisibleHost existingHost in sortedHosts)
            {
                stackHostList.Children.Add(existingHost);
            }
        }


        /// <summary>
        /// Used for sorting hosts in hosts list
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        private int CompareHostsForList(UI.VisibleHost a, UI.VisibleHost b)
        {
            if (a.HasNickname && !b.HasNickname)
            {
                return -1;
            }
            else if (!a.HasNickname && b.HasNickname)
            {
                return 1;
            }
            else
            {
                return a.Mac.CompareTo(b.Mac);
            }
        }
        /// <summary>
        /// Load an image from the indicated embedded resource
        /// </summary>
        /// <param name="resourceName">Path to an embedded PNG resource</param>
        /// <returns></returns>
        private ImageSource LoadImage(string resourceName)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            BitmapImage bitmap = new BitmapImage();

            using (System.IO.Stream stream =
                assembly.GetManifestResourceStream(resourceName))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }

            return bitmap;
        }

        private void expanderHostsList_Collapsed(object sender, RoutedEventArgs e)
        {

        }

        private void expanderHostsList_Expanded(object sender, RoutedEventArgs e)
        {
            UserExpandedHost?.Invoke(this);
            PopulateHostList();
        }
        /// <summary>
        /// Check hosts that have been seen and update hosts list
        /// </summary>
        private void PopulateHostList()
        {
            foreach (KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> host in hosts)
            {
                ulong macInt = host.Key.ToValue();
                if(hostControlsAdded.ContainsKey(macInt))
                {
                    // IP address may have changed; update it just in case
                    hostControlsAdded[macInt].IpV4Address = host.Value.HostIpV4Address;
                } else
                {
                    // Need to add to list
                    int insertIdx = GetIndexAtWhichToInserHost(macInt);
                    VisibleHost hostControl = new VisibleHost(host.Value);
                    stackHostList.Children.Insert(insertIdx, hostControl);
                    hostControlsAdded.Add(macInt, hostControl);
                }
            }
        }

        /// <summary>
        /// Find the index at which to insert a host that is not already in the list.
        /// Based on a binary search (with the assumption that host is not present).
        /// </summary>
        /// <param name="macInt">Integer form of mac address of host</param>
        /// <returns>Index at which to insert into </returns>
        private int GetIndexAtWhichToInserHost(ulong macInt)
        {
            int lowIdx = 0;
            int highIdx = stackHostList.Children.Count - 1;
            while (lowIdx <= highIdx)
            {
                int midIdx = (lowIdx + highIdx) / 2;
                ulong midMacInt = (stackHostList.Children[midIdx] as VisibleHost).MacInt;
                if (midMacInt > macInt)
                {
                    highIdx = midIdx - 1;
                }
                else // We know they aren't equal
                {
                    lowIdx = midIdx + 1;
                }
            }
            return lowIdx;
        }
    }
}
