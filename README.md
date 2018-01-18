# passiveAgressor
Lists out the hosts that can be determined by passively listening to network traffic.

# Installation
1. Install Java
1. If on Windows, install [WinPcap](https://www.winpcap.org/install/)
    * Note - if you've installed Wireshark, this step is already completed
2. Download or clone this repository

# Usage
* Run `passiveAgressor\passiveAggressor\dist\run.bat` if you're on Windows
* Run `passiveAgressor\passiveAggressor\dist\run.sh` if you're on Linux

Sample output:
````
Known hosts:
Hardware address        IP address      Interface manufacturer
00:24:B2:88:DC:8B       173.194.206.189 NETGEAR
B8:86:87:FD:50:33       10.0.0.14       Liteon Technology Corporation
````
