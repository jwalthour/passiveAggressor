/**
 * This class is used to maintain and query on the set of hosts observed by the application.
 * 
 * 
 */
package passiveAggressor;

import java.util.TreeSet;

public class HostManager {
	private TreeSet<Host> hosts = new TreeSet<>(); // Default sort compares by MAC address
	
	/**
	 * Update storage to indicate that a packet has been seen from this host.
	 * @param macAddr an array of 6 bytes
	 * @param ipAddr an array of 4 bytes or 8 16bit numbers
	 */
	public void noteHost(int[] macAddr, int[] ipAddr) {
		noteHost(new Host(macAddr, ipAddr));
	}
	
	/**
	 * Update storage to indicate that a packet has been seen from this host.
	 * @param host
	 */
	public void noteHost(Host host) {
		// Check if we already have a host object matching the description
		Host mostSimilarHost = hosts.floor(host);
		if(mostSimilarHost.equals(host)) {
			mostSimilarHost.noteSeen();
		} else {
			// This is a new one
			hosts.add(host);
		}
	}
}
