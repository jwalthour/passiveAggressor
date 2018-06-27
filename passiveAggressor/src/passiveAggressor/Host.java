/**
 * This class stores information about an observed host
 * 
 */
package passiveAggressor;

import java.util.Arrays;

public class Host implements Comparable {
	// Identity of host
	private int[] macAddr = null;
	private int[] ipV4Addr = null;
	private int[] ipV6Addr = null;
	
	// Other metadata about host
	private int numTimesSeen = 0;
	// TODO: Last time seen 

	/**
	 * Constructor that sets all addresses
	 * @param macAddr
	 * @param ipV4Addr
	 * @param ipV6Addr
	 */
	public Host(int[] macAddr, int[] ipV4Addr, int[] ipV6Addr) {
		setMacAddr(macAddr);
		setIpV4Addr(ipV4Addr);
		setIpV6Addr(ipV6Addr);
	}

	/**
	 * Constructor that auto-guesses which kind of IP address was given
	 * @param macAddr
	 * @param ipAddr
	 */
	public Host(int[] macAddr, int[] ipAddr) {
		setIpAddr(ipAddr);
		setMacAddr(macAddr);
	}

	/**
	 * Default constructor
	 */
	public Host() {
		this(null, null, null);
	}
	
	/**
	 * Call this to record that a packet from this host was seen on the network
	 */
	public void noteSeen() {
		numTimesSeen++;
	}

	/**
	 * 
	 * @return true if this host's IP address is reserved for link-local
	 *         routing, false otherwise.
	 */
	public boolean isInternal() {
		if (ipV6Addr != null) {
			return (ipV6Addr[0] & 0xFFD0) == 0xFF80;
		} else if (ipV4Addr != null) {
			if (ipV4Addr[0] == 10) {
				return true;
			} else if (ipV4Addr[0] == 172 && ((ipV4Addr[1] & 240) == 16)) {
				return true;
			} else if (ipV4Addr[0] == 192 && ipV4Addr[1] == 168) {
				return true;
			} else {
				return false;
			}
		} else {
			// No idea
			throw new NullPointerException("No IP addresses set.");
		}
	}

	public int[] getMacAddr() {
		return macAddr;
	}

	public void setMacAddr(int[] macAddr) {
		this.macAddr = macAddr.clone();
	}

	/**
	 * 
	 * @return The highest-version IP address, or null.
	 */
	public int[] getIpAddr() {
		if (ipV6Addr != null) {
			return ipV6Addr;
		} else if (ipV4Addr != null) {
			return ipV4Addr;
		} else {
			return null;
		}
	}
	
	public void setIpAddr(int[] ipAddr) {
		if(ipAddr == null) {
			// Do nothing
		} else if(ipAddr.length == 4) {
			setIpV4Addr(ipAddr);
		} else if (ipAddr.length == 8) {
			setIpV6Addr(ipAddr);
		} else {
			throw new IllegalArgumentException("ipAddr must be an array of 4 bytes or of 8 16bit integers.");
		}
	}

	public int[] getIpV4Addr() {
		return ipV4Addr;
	}

	public void setIpV4Addr(int[] ipAddr) {
		if (ipAddr == null) {
			this.ipV4Addr = null;
		} else {
			if (ipAddr.length != 4) {
				throw new IllegalArgumentException("An IPv4 address must be an array of 4 8bit numbers.");
			} else {
				this.ipV4Addr = ipAddr.clone();
			}
		}
	}

	public int[] getIpV6Addr() {
		return ipV6Addr;
	}

	public void setIpV6Addr(int[] ipAddr) {
		if (ipAddr == null) {
			this.ipV6Addr = null;
		} else {
			if (ipAddr.length != 8) {
				throw new IllegalArgumentException("An IPv6 address must be an array of 8 16bit numbers.");
			} else {
				this.ipV6Addr = ipAddr.clone();
			}
		}
	}

	@Override
	/**
	 * @return true if these two hosts represent the same machine
	 */
	public boolean equals(Object other) {
		if (other instanceof Host) {
			return Arrays.equals(this.macAddr, (((Host) other).macAddr));
		} else {
			throw new IllegalArgumentException();
		}
	}

	@Override
	/**
	 * @return a java hashcode for this machine
	 */
	public int hashCode() {
		return Arrays.hashCode(macAddr);
	}

	@Override
	public int compareTo(Object other) {
		return compareByMac(other);
	}

	public int compareByMac(Object other) {
		if (other instanceof Host) {
			return compareArrays(this.macAddr, ((Host) other).macAddr);
		} else {
			throw new IllegalArgumentException();
		}
	}

	public int compareByIp(Object other) {
		if (other instanceof Host) {
			return compareArrays(this.getIpAddr(), ((Host) other).getIpAddr());
		} else {
			throw new IllegalArgumentException();
		}
	}

	/**
	 * Compare two arrays of ints, for sorting.
	 * @param a
	 * @param b
	 * @return 0 if arrays are equal, -1 if a should be first, 1 if b should be first.
	 *   We say longer arrays should be later in a list, if two arrays otherwise match.
	 */
	public static int compareArrays(int[] a, int[] b) {
		for (int i = 0; i < a.length; ++i) {
			if (i >= b.length) {
				return 1; // a is longer and they were equal
			} else {
				if (a[i] > b[i]) {
					return 1;
				} else if (a[i] > b[i]) {
					return -1;
				}
			}
		}
		// Arrays were equal up to this point
		if (a.length == b.length) {
			return 0; // arrays are totally equal
		} else {
			return -1; // b is longer
		}
	}
}
