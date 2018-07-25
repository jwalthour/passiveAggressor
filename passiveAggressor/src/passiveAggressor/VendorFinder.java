package passiveAggressor;

import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class VendorFinder {
	public static final int PREFIX_BYTES = 3;
	private HashMap<Integer, String> vendorForMacPrefix = new HashMap<>();
	
	/*
	 * Loads a list of vendors from the IEEE OUI.
	 * Assumes filename has been downloaded from standards-oui.ieee.org/oui.txt
	 * 
	 */
	public void loadOui(String filepath) throws FileNotFoundException, IOException {
		BufferedReader reader = new BufferedReader(new FileReader(filepath));
	    String line;
	    
    	// Looking for all lines like 
    	//E0-43-DB   (hex)		Shenzhen ViewAt Technology Co.,Ltd. 
	    String pattern = "([A-Z0-9][A-Z0-9])-([A-Z0-9][A-Z0-9])-([A-Z0-9][A-Z0-9])   \\(hex\\)\t\t(.+)$";
	    Pattern regex = Pattern.compile(pattern);

	    try {
		    while ((line = reader.readLine()) != null) {
			    Matcher m = regex.matcher(line);
		    	if(m.find()) {
		    		String name = m.group(4);
		    		int val = 0;
		    		for(int i = 0; i < PREFIX_BYTES; ++i) {
		    			int tempVal = Integer.parseInt(m.group(1 + i).toLowerCase(), 16); 
		    			val += (tempVal << (8*(PREFIX_BYTES-i-1))); 
		    		}
		    		
		    		vendorForMacPrefix.put(val, name);
		    	}	    	
		    }
	    } finally {
	    	reader.close();
	    }
	}
	
	public static int repPrefixAsInt(byte[] prefix) {
		int val = 0;
		for(int i = 0; i < PREFIX_BYTES; ++i) {
			// Java, for reasons unknown, insists on making every type signed.
			// Thus the pcap library must represent values 0-255 using
			// the values -128 to 127.
			int tempVal = prefix[i]; 
			if(tempVal < 0) { tempVal += 256; }
			val += (tempVal << (8*(PREFIX_BYTES-i-1)));
		}
		return val;
	}
	
	public String getMfrName(byte[] prefix) {
		return vendorForMacPrefix.get(repPrefixAsInt(prefix));
	}
	
	
	public String getMfrName(int[] prefix) {
		return getMfrName(new byte[] {(byte) prefix[0], (byte) prefix[1], (byte) prefix[2]});
	}
	
	public String getMfrName(int prefix) {
		return vendorForMacPrefix.get(prefix);
	}
}
