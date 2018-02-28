package passiveAggressor;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.ArrayList;  
import java.util.HashMap;
import java.util.List;  
  
import org.jnetpcap.Pcap;
import org.jnetpcap.PcapAddr;
import org.jnetpcap.PcapIf;  
import org.jnetpcap.packet.PcapPacket;  
import org.jnetpcap.packet.PcapPacketHandler;
import org.jnetpcap.protocol.lan.Ethernet;
import org.jnetpcap.protocol.network.Ip4;  
  
import org.apache.commons.cli.*;

public class PassiveAggressor {

	VendorFinder vf;
	
	public static void main(String[] args) {
		Options cmdLineOpts = new Options();
		Option captureDevOpt = new Option("d", "device", true, "Index of interface to monitor");
		captureDevOpt.setRequired(true);
		cmdLineOpts.addOption(captureDevOpt);
		Option mfrOfInterestOpt = new Option("m", "mfr", true, "Manufacturer of interest");
		mfrOfInterestOpt.setRequired(false);
		cmdLineOpts.addOption(mfrOfInterestOpt);
		Option ouiPathOpt = new Option("o", "oui", true, "Path to OUI file");
		ouiPathOpt.setRequired(false);
		cmdLineOpts.addOption(ouiPathOpt);
		

        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(cmdLineOpts, args);
        } catch (ParseException e) {
            System.out.println(e.getMessage());
            formatter.printHelp("passiveAggressor", cmdLineOpts);

            System.exit(1);
            return;
        }

        String captureDeviceIndexStr = cmd.getOptionValue(captureDevOpt.getLongOpt());
        int captureDeviceIndex = 0;
        try {
        	captureDeviceIndex = Integer.parseInt(captureDeviceIndexStr);
        	if(captureDeviceIndex < 0 ) {
        		throw new NumberFormatException();
        	}
        } catch (NumberFormatException e) {
        	System.out.println("Can't parse the provided device number.  Please provide an integer greater than or equal to 0.");
        }
        
        PassiveAggressor pa = new PassiveAggressor();
        pa.loadOui("../data/oui.txt");
		
		try {
			pa.listen(captureDeviceIndex);
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

	}
	
	public void printListOfAdapters() throws IOException {
        List<PcapIf> alldevs = new ArrayList<PcapIf>(); // Will be filled with NICs  
        StringBuilder errbuf = new StringBuilder(); // For any error msgs  
        int r = Pcap.findAllDevs(alldevs, errbuf);  
        if (r != Pcap.OK || alldevs.isEmpty()) {  
            System.err.printf("Discovered no devices; error is %s", errbuf  
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
            System.out.printf("#%d: %s (%s) [%s] (%s) \n", i++, device.getName(), (mfr != null? mfr : "Unknown manufacturer"), description, addrString);  
        }  
        
	}
	
	public void loadOui(String filepath) {
		vf = new VendorFinder();
		try {
			vf.loadOui(filepath);
		} catch (FileNotFoundException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
	
	public void listen(int interfaceIndex) throws IOException {
        List<PcapIf> alldevs = new ArrayList<PcapIf>(); // Will be filled with NICs  
        StringBuilder errbuf = new StringBuilder(); // For any error msgs  

        /*************************************************************************** 
         * First get a list of devices on this system 
         **************************************************************************/  
        int r = Pcap.findAllDevs(alldevs, errbuf);  
        if (r != Pcap.OK || alldevs.isEmpty()) {  
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
  
        PcapIf device = alldevs.get(interfaceIndex);
        System.out.printf("\nListening passively on '%s'.:\n",  
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
        
        long interfaceAddr = repByteArrayAsInt(device.getHardwareAddress());
        
  
        // Closure
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
                if(packet.hasHeader(Ip4.ID, 0)) {
                	Ip4 ip = new Ip4();
                	packet.getHeader(ip);
                	sourceIpAddr = repByteArrayAsIntArray(ip.source());
                } // TODO: IPv6 support
                
                if(sourceMacAddr != null && sourceIpAddr != null) {
//                	System.out.println("Got usable packet: " + repArrayAsString(sourceMacAddr, ':', true) + "-----" + sourceIpStr);
                	long mac = repIntArrayAsInt(sourceMacAddr);
                	if(mac != interfaceAddr) {
//	                	if(macToIpMapping.containsKey(mac)) {
//	                		// TODO: Update count
//	                	} else {
	                		macToIpMapping.put(mac, sourceIpAddr);
	                		System.out.println("\nKnown hosts:");
	                		printMapping(macToIpMapping, vf);
//	                	}
                	}
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
        pcap.loop(Pcap.LOOP_INFINITE, jpacketHandler, "");  
  
        /*************************************************************************** 
         * Last thing to do is close the pcap handle 
         **************************************************************************/  
        pcap.close();  
    }

	public static void printMapping(HashMap<Long, int[]> map, VendorFinder vf) {
		System.out.println("Hardware address\tIP address \tInterface manufacturer");
		for (Long mac : map.keySet()) {
			int[] macArr = repIntAsArray(mac, 6);
			int prefix = getPrefixFromMac(mac);
			int[] ipArr  = map.get(mac);
			String macString = repArrayAsString(macArr, ':', true);
			char sep = (ipArr.length > 4)? ':' : '.';
			String ipString  = repArrayAsString(ipArr, sep, ipArr.length > 4);
			String mfrString = vf.getMfrName(prefix);
			System.out.println(macString + "\t" + ipString + "\t" + mfrString);
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
	
	public static int getPrefixFromMac(long mac) {
		return (int)((0xffffff000000l & mac) >> 24);
	}

	public static String repArrayAsString(int[] addr, char sep, boolean hex) {
		
		String ret = (hex? String.format("%02X", addr[0]) : Integer.toString(addr[0]));
		for(int i = 1; i < addr.length; ++i) {
			ret += sep + (hex? String.format("%02X", addr[i]) : Integer.toString(addr[i]));
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
