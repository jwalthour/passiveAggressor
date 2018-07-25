package passiveAggressor;

public class Conversions {

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
