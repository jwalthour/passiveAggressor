package passiveAggressor;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.ArrayList;  
import java.util.Date;
import java.util.HashMap;
import java.util.List;  
  
import org.jnetpcap.Pcap;
import org.jnetpcap.PcapAddr;
import org.jnetpcap.PcapIf;  
import org.jnetpcap.packet.PcapPacket;  
import org.jnetpcap.packet.PcapPacketHandler;
import org.jnetpcap.packet.structure.JField;
import org.jnetpcap.protocol.lan.Ethernet;
import org.jnetpcap.protocol.network.Arp;
import org.jnetpcap.protocol.network.Icmp;
import org.jnetpcap.protocol.network.Ip4;
import org.jnetpcap.protocol.network.Ip6;
import org.jnetpcap.protocol.tcpip.Tcp;
import org.jnetpcap.protocol.tcpip.Udp;  
  
public class PassiveAggressor {

	public static void main(String[] args) {
		try {
			adapterTest();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

	}
	
	private static void adapterTest() throws IOException {
        List<PcapIf> alldevs = new ArrayList<PcapIf>(); // Will be filled with NICs  
        StringBuilder errbuf = new StringBuilder(); // For any error msgs  
  
		VendorFinder vf = new VendorFinder();
		try {
			vf.loadOui("../data/oui.txt");
		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

        /*************************************************************************** 
         * First get a list of devices on this system 
         **************************************************************************/  
        int r = Pcap.findAllDevs(alldevs, errbuf);  
        if (r == Pcap.NOT_OK || alldevs.isEmpty()) {  
            System.err.printf("Can't read list of devices, error is %s", errbuf  
                .toString());  
            return;  
        }  
  
        System.out.println("Network devices found:");  
  
        int i = 0;  
        for (PcapIf device : alldevs) {  
        	List<PcapAddr> addrs = device.getAddresses();
            String description =  
                (device.getDescription() != null) ? device.getDescription()  
                    : "No description available";  
            String addrString = "";
            if(addrs.size() > 0) {
            	addrString = addrs.get(0).getAddr().toString();
            }
//            device.getHardwareAddress();
            String mfr = "";
            byte[] prefix = {device.getHardwareAddress()[0],device.getHardwareAddress()[1],device.getHardwareAddress()[2]}; 
            mfr = vf.getMfrName(prefix);
            System.out.printf("#%d: %s (%s) [%s] (%s) \n", i++, device.getName(), mfr, description, addrString);  
        }  
  
        PcapIf device = alldevs.get(0); // We know we have atleast 1 device  
        System.out  
            .printf("\nChoosing '%s' on your behalf:\n",  
                (device.getDescription() != null) ? device.getDescription()  
                    : device.getName());  
  
        /*************************************************************************** 
         * Second we open up the selected device 
         **************************************************************************/  
        int snaplen = 64 * 1024;           // Capture all packets, no trucation  
        int flags = Pcap.MODE_PROMISCUOUS; // capture all packets  
        int timeout = 10 * 1000;           // 10 seconds in millis  
        Pcap pcap =  
            Pcap.openLive(device.getName(), snaplen, flags, timeout, errbuf);  
  
        if (pcap == null) {  
            System.err.printf("Error while opening device for capture: "  
                + errbuf.toString());  
            return;  
        }  
        
  
        // Closure?
        HashMap<Long, int[]> macToIpMapping = new HashMap<>();
        /*************************************************************************** 
         * Third we create a packet handler which will receive packets from the 
         * libpcap loop. 
         **************************************************************************/  
        PcapPacketHandler<String> jpacketHandler = new PcapPacketHandler<String>() {  
  
            public void nextPacket(PcapPacket packet, String user) {  
  
//                System.out.printf("Received packet at %s caplen=%-4d len=%-4d %s\n",  
//                    new Date(packet.getCaptureHeader().timestampInMillis()),   
//                    packet.getCaptureHeader().caplen(),  // Length actually captured  
//                    packet.getCaptureHeader().wirelen(), // Original length   
//                    user                                 // User supplied object  
//                    );  
                
                // Store hardware address
                int[] sourceMacAddr = null;
                if(packet.hasHeader(Ethernet.ID)) {
	            	Ethernet eth = new Ethernet();
	            	packet.getHeader(eth);
	            	sourceMacAddr = repByteArrayAsIntArray(eth.source());
                } // TODO: What the heck does jnetcap call 802.11?  There's 801.1q... what the heck is q?

                
                // Store IP address
                int[] sourceIpAddr = null;
                String sourceIpStr = "";
                if(packet.hasHeader(Ip4.ID, 0)) {
                	Ip4 ip = new Ip4();
                	packet.getHeader(ip);
                	sourceIpAddr = repByteArrayAsIntArray(ip.destination());
                	sourceIpStr  = repArrayAsString(sourceIpAddr, '.', false);
//                } else if(packet.hasHeader(Ip6.ID, 0)) {
//                	Ip6 ip = new Ip6();
//                	packet.getHeader(ip);
//                	sourceIpAddr = repByteArrayAsIntArray(ip.destination());
//                	sourceIpStr  = repArrayAsString(sourceIpAddr, ':', true);
                }
                
                if(sourceMacAddr != null && sourceIpAddr != null) {
//                	System.out.println("Got usable packet: " + repArrayAsString(sourceMacAddr, ':', true) + "-----" + sourceIpStr);
                	
                	macToIpMapping.put(repIntArrayAsInt(sourceMacAddr), sourceIpAddr);
                }
            }  
        };  
  
        /*************************************************************************** 
         * Fourth we enter the loop and tell it to capture 10 packets. The loop 
         * method does a mapping of pcap.datalink() DLT value to JProtocol ID, which 
         * is needed by JScanner. The scanner scans the packet buffer and decodes 
         * the headers. The mapping is done automatically, although a variation on 
         * the loop method exists that allows the programmer to sepecify exactly 
         * which protocol ID to use as the data link type for this pcap interface. 
         **************************************************************************/  
        pcap.loop(10, jpacketHandler, "jNetPcap rocks!");  
  
        printMapping(macToIpMapping);
        
        /*************************************************************************** 
         * Last thing to do is close the pcap handle 
         **************************************************************************/  
        pcap.close();  
    }

	public static void printMapping(HashMap<Long, int[]> map) {
		for (Long mac : map.keySet()) {
			int[] macArr = repIntAsArray(mac, 6);
			int[] ipArr  = map.get(mac);
			String macString = repArrayAsString(macArr, ':', true);
			char sep = (ipArr.length > 4)? ':' : '.';
			String ipString  = repArrayAsString(ipArr, sep, ipArr.length > 4);
			System.out.println(macString + " is at " + ipString);
		}
	}

	public static int repByteArrayAsInt(byte[] addr) {
		int val = 0;
		for(int i = 0; i < addr.length; ++i) {
			// Java, for reasons unknown, insists on making every type signed.
			// Thus the pcap library must represent values 0-255 using
			// the values -128 to 127.
			int tempVal = addr[i]; 
			if(tempVal < 0) { tempVal += 256; }
			val += ((long)tempVal << (8*(addr.length-i-1)));
		}
		return val;
	}

	public static long repIntArrayAsInt(int[] addr) {
		long val = 0;
		for(int i = 0; i < addr.length; ++i) {
			// Assumes these are all 0-255
			val += ((long)addr[i] << (8*(addr.length - i - 1)));
		}
		return val;
	}

	public static String repArrayAsString(int[] addr, char sep, boolean hex) {
		
		String ret = (hex? Integer.toHexString(addr[0]) : Integer.toString(addr[0]));
		for(int i = 1; i < addr.length; ++i) {
			ret += sep + (hex? Integer.toHexString(addr[i]) : Integer.toString(addr[i]));
		}
		return ret;
	}
	
	public static int[] repIntAsArray(long value, int size) {
		// Assumes it was assembled from numbers 0-255
		int[] ret = new int[size];
		for(int i = 0; i < size; ++i) {
			long tempVal = (value >> (8*(size - i - 1))) & 0x000000FF;
			ret[i] = (int)tempVal;
		}
		return ret;
	}
	
	public static int[] repByteArrayAsIntArray(byte[] addr) {
		int val[] = new int[addr.length];
		for(int i = 0; i < addr.length; ++i) {
			// Java, for reasons unknown, insists on making every type signed.
			// Thus the pcap library must represent values 0-255 using
			// the values -128 to 127.
			int tempVal = addr[i]; 
			if(tempVal < 0) { tempVal += 256; }
			val[i] = tempVal;
		}
		return val;
	}
	
}
