/**
 * This class stores information about an observed host
 * 
 */
package passiveAggressor;

public class Host {
	private int[] macAddr = null;
	private int[] ipV4Addr = null;
	private int[] ipV6Addr = null;

	public Host(int[] macAddr, int[] ipV4Addr, int[] ipV6Addr) {
		setMacAddr(macAddr);
		setIpV4Addr(ipV4Addr);
		setIpV6Addr(ipV6Addr);
	}
	
	public Host(int[] macAddr, int[] ipV4Addr) {
		this(macAddr, ipV4Addr, null);
	}
	
	public Host() {
		this(null, null);
	}
	
	/**
	 * 
	 * @return true if this host's IP address is reserved
	 * 		for link-local routing, false otherwise.
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
		if(ipV6Addr != null) {
			return ipV6Addr;
		} else if (ipV4Addr != null) {
			return ipV4Addr;
		} else {
			return null;
		}
	}
	public int[] getIpV4Addr() {
		return ipV4Addr;
	}
	public void setIpV4Addr(int[] ipAddr) {
		if(ipAddr == null) {
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

}
