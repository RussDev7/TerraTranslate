/// GetPortsFromProcessName - By: RussDev7
/// https://github.com/RussDev7/GetPortsFromProcessName
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace TerraTranslate
{
    class TcpConnectionInfo
    {
        public enum TCP_TABLE_CLASS : int
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public int owningPid;

            public ushort LocalPort
            {
                get
                {
                    return BitConverter.ToUInt16(
                        new byte[2] { localPort2, localPort1 }, 0);
                }
            }

            public ushort RemotePort
            {
                get
                {
                    return BitConverter.ToUInt16(
                        new byte[2] { remotePort2, remotePort1 }, 0);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            MIB_TCPROW_OWNER_PID table;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetExtendedTcpTable(IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            int reserved);

        public static MIB_TCPROW_OWNER_PID[] GetAllTcpConnections()
        {
            MIB_TCPROW_OWNER_PID[] tTable;
            int AF_INET = 2;    // IP_v4
            int buffSize = 0;

            // how much memory do we need?
            uint ret = GetExtendedTcpTable(IntPtr.Zero,
                ref buffSize,
                true,
                AF_INET,
                TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                0);
            if (ret != 0 && ret != 122) // 122 insufficient buffer size
                throw new Exception("bad ret on check " + ret);
            IntPtr buffTable = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedTcpTable(buffTable,
                    ref buffSize,
                    true,
                    AF_INET,
                    TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                    0);
                if (ret != 0)
                    throw new Exception("bad ret " + ret);
                // get the number of entries in the table
                MIB_TCPTABLE_OWNER_PID tab =
                    (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(buffTable, typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr rowPtr = (IntPtr)((long)buffTable +
                    Marshal.SizeOf(tab.dwNumEntries));
                tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];

                for (int i = 0; i < tab.dwNumEntries; i++)
                {
                    MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    tTable[i] = tcpRow;
                    // next entry
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            return tTable;
        }

        // Get local port for process name.
        public static ushort TryGetLocalPort(string processName, out ushort localPort)
        {
            // Ensure the process exists.
            var proccesses = Process.GetProcessesByName(processName);

            // Preccess does not exist, return zero.
            if (!proccesses.Any())
                return localPort = 0;

            // Get each process.
            foreach (var process in TcpConnectionInfo.GetAllTcpConnections())
            {
                // Check if the process PID is the same as the process PID.
                if (process.owningPid == proccesses[0].Id)
                {
                    return localPort = process.LocalPort;
                }
            }

            // No value found, return zero.
            return localPort = 0;
        }

        // Get local port for process name.
        public static ushort TryGetRemotePort(string processName, out ushort remotePort)
        {
            // Ensure the process exists.
            var proccesses = Process.GetProcessesByName(processName);

            // Preccess does not exist, return zero.
            if (!proccesses.Any())
                return remotePort = 0;

            // Get each process.
            foreach (var process in TcpConnectionInfo.GetAllTcpConnections())
            {
                // Check if the process PID is the same as the process PID.
                if (process.owningPid == proccesses[0].Id)
                {
                    return remotePort = process.RemotePort;
                }
            }

            // No value found, return zero.
            return remotePort = 0;
        }
    }
}
