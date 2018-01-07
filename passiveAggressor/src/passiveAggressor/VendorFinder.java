package passiveAggressor;

import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.Reader;
import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class VendorFinder {
	private HashMap<byte[], String> vendorForMacPrefix;
	
	/*
	 * Loads a list of vendors from the IEEE OUI.
	 * Assumes filename has been downloaded from standards-oui.ieee.org/oui.txt
	 * 
	 */
	public void loadOui(String filepath) throws FileNotFoundException, IOException {
		BufferedReader reader = new BufferedReader(new FileReader(filepath));
	    String line;
	    
	    line = "testing";
	    String pattern = "([A-Z0-9][A-Z0-9])-([A-Z0-9][A-Z0-9])-([A-Z0-9][A-Z0-9])   \\(hex\\)\t\t(.+)$";
	    Pattern regex = Pattern.compile(pattern);

	    while ((line = reader.readLine()) != null) {
	    	// Looking for all lines like 
	    	//E0-43-DB   (hex)		Shenzhen ViewAt Technology Co.,Ltd. 
		    Matcher m = regex.matcher(line);
	    	if(m.find()) {
	    		System.out.println("Got a match: " + m.group(1) + ":" + m.group(2) + ":" + m.group(3) + " (" + m.group(4) + ")");
	    	}
	    }
		
	}
}
