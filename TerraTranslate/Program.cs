using System;
using System.Text;
using SharpPcap;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;
using PacketDotNet;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;
using System.Threading.Tasks;

namespace TerraTranslate
{
    public class Program
    {
        #region DllImports & Variables

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner  
            public int Top;         // y position of upper-left corner  
            public int Right;       // x position of lower-right corner  
            public int Bottom;      // y position of lower-right corner  
        }

        public static string tanslateFromLanguage = "zh"; // Chinese.
        public static string tanslateToLanguage = "en"; // English.
        public static int gamePort = 0; // Define the local game port.
        public static ILiveDevice packetDevice;
        private static Timer chatTimer = null;
        public static string lastMessage = "";
        public static int messageOffset = 0;
        public static bool showChat = false;
        #endregion

        #region Initial Configuration

        public static void Main()
        {
            // Switch the console enocding.
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Print SharpPcap version
            // var ver = Pcap.SharpPcapVersion;

            // Retrieve the device list

            // If no devices were found print an error
            if (CaptureDeviceList.Instance.Count < 1)
            {
                Console.WriteLine("ERROR: No devices were found on this machine");
                return;
            }

            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            int i = 0;

            // Scan the list printing every entry
            foreach (var dev in CaptureDeviceList.Instance)
            {
                Console.WriteLine("{0}) {1}", i, dev.Description);
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");

            // Ensure choice was valid.
            var choice = Console.ReadLine();
            if (choice != "")
                i = int.Parse(choice);
            else
                i = 0;

            // Define new capture device.
            using var packetDevice = CaptureDeviceList.New()[i];

            // Choose a language to translate.
            Console.WriteLine();
            Console.WriteLine("The following language translations are available:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("0) ZH <-> EN");
            Console.WriteLine("1) EN <-> ZH");
            Console.WriteLine("2) RU <-> EN");
            Console.WriteLine("3) EN <-> RU");
            Console.WriteLine("4) ES <-> EN");
            Console.WriteLine("5) EN <-> ES");
            Console.WriteLine();

            switch (Console.ReadLine())
            {
                case "0":
                    tanslateFromLanguage = "zh";
                    tanslateToLanguage = "en";
                    break;
                case "1":
                    tanslateFromLanguage = "en";
                    tanslateToLanguage = "zh";
                    break;
                case "2":
                    tanslateFromLanguage = "ru";
                    tanslateToLanguage = "en";
                    break;
                case "3":
                    tanslateFromLanguage = "en";
                    tanslateToLanguage = "ru";
                    break;
                case "4":
                    tanslateFromLanguage = "es";
                    tanslateToLanguage = "en";
                    break;
                case "5":
                    tanslateFromLanguage = "en";
                    tanslateToLanguage = "es";
                    break;
                default:
                    tanslateFromLanguage = "zh";
                    tanslateToLanguage = "en";
                    break;
            }

            //Register our handler function to the 'packet arrival' event
            packetDevice.OnPacketArrival +=
                new PacketArrivalEventHandler(Device_OnPacketArrival);

            //Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            packetDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);

            // Get the local port terraria is running on.
            ushort terrariaPort = TcpConnectionInfo.TryGetLocalPort("Terraria", out ushort value);
            if (terrariaPort == 0)
            {
                Console.WriteLine("ERROR: Game is currently not running.");
                return;
            }

            // Define the local gameport for filtering.
            gamePort = terrariaPort;

            // Send start message to console.
            Console.Clear();
            Console.WriteLine("-- TerraTranslate v1.1 by discord:dannyruss is now starting...");
            Console.WriteLine
                ("-- The following tcpdump filter will be applied: tcp dst port " + terrariaPort);
            Console.WriteLine
                ("-- Listening on {0}, hit 'Ctrl-C' to exit...",
                packetDevice.Description);
            Console.WriteLine("----------------------------------------------------");

            // Write new entree to file.
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\TranslatedServerLog.txt", "[*] Joining a new server! Eastablishing a new connection: " + ("tcp dst port " + terrariaPort) + Environment.NewLine);
            lastMessage = "[*] Joining a new server! Eastablishing a new connection: " + ("tcp dst port " + terrariaPort);

            chatTimer = new Timer(ChatTimer, null, 0, 1);

            // Start capture packets.
            packetDevice.Capture();
        }
        #endregion

        #region Revieve Packets

        // Get packets.
        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            // Parse packetcapture into a packet.
            var p = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
            if (p.PayloadPacket is IPv4Packet ipPacket) // Ensure packet is IPv4.
            {
                if (ipPacket.PayloadPacket is TcpPacket tcpPacket) // Ensure packet is TCP.
                {
                    // Grab desired packets based on src or dst.
                    if (tcpPacket.SourcePort == gamePort && Convert.ToHexString(e.Data).Contains("2F73686F77")) // Grab commands - "/show"
                    {
                        // Show the chat.
                        showChat = true;

                        // Send console message.
                        Console.WriteLine("[CMD] Turned on in-game chat!");
                        lastMessage = "[CMD] Turned on in-game chat!";
                        return;
                    }
                    else if (tcpPacket.SourcePort == gamePort && Convert.ToHexString(e.Data).Contains("2F68696465")) // Grab commands - "/hide"
                    {
                        // Show the chat.
                        showChat = false;

                        // Send console message.
                        Console.WriteLine("[CMD] Turned off in-game chat!");
                        lastMessage = "[CMD] Turned off in-game chat!";
                        return;
                    }
                    else if (tcpPacket.SourcePort == gamePort && Convert.ToHexString(e.Data).Contains("2F6C616E67")) // Grab commands - "/lang"
                    {
                        // Gather message to collect.
                        var ingameCommandText = Encoding.UTF8.GetString(FromHex(Convert.ToHexString(e.Data).Split("2F6C616E67")[1]));

                        // Ensure text is not empty.
                        if (ingameCommandText != "")
                            ingameCommandText = ingameCommandText[1..];
                        else
                        {
                            Console.WriteLine("[Clipboard] ERROR: The input text was blank!");
                            lastMessage = "[Clipboard] ERROR: The input text was blank!";
                            return;
                        }

                        // Translate message to opposing language.
                        var textTranslated = Translate(ingameCommandText, tanslateToLanguage, tanslateFromLanguage);

                        // Save translated message to clipboard.
                        WindowsClipboard.SetText(textTranslated);

                        // Anounce to console.
                        Console.WriteLine("[Clipboard] Translated: \"" + ingameCommandText + "\" -> \"" + textTranslated + "\".");

                        // Save last text.
                        lastMessage = "[Clipboard] Translated: \"" + ingameCommandText + "\" -> \"" + textTranslated + "\".";

                        // Switch new chat to the next placement.
                        ChatPlacementToggle();
                    }
                    else if (tcpPacket.DestinationPort == gamePort && Convert.ToHexString(e.Data).Contains("00520100")) // Filter only chat messages. // Raw: " R? "
                    {
                        // Get header time.
                        var time = e.Header.Timeval.Date;

                        // Get lengh.
                        var lengh = e.Data.Length;

                        // Message in hex.
                        // var originalMessage = FromHex(Convert.ToHexString(e.Data));

                        // Gather message to translate. // Old - 00520100.
                        // var messageFirstHalf = Convert.ToHexString(e.Data).Split("00520100")[0] + "00520100"; // Add 00520100 back onto the string.
                        var messageLastHalf = Convert.ToHexString(e.Data).Split("00520100")[1];

                        // Convert message text to UTF8 and trim. Trim first 3 and last three chars.
                        var messageTextUTF8 = Encoding.UTF8.GetString(FromHex(messageLastHalf))[3..][..^3];

                        // Do Translate stuff.
                        var translatedText = Translate(messageTextUTF8, tanslateFromLanguage, tanslateToLanguage);

                        // Convert string back to original.
                        // var messageTranslatedHex = messageFirstHalf + BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(translatedText)).Replace("-", "");
                        // var messageTranslatedByte = FromHex(messageTranslatedHex);

                        // Display message.
                        File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\TranslatedServerLog.txt", "Time=" + time.Hour + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond + ", Data=" + translatedText + ", Lengh=" + lengh + ", Hex: " + messageLastHalf + Environment.NewLine);
                        Console.WriteLine(translatedText);

                        // Save last text.
                        if (!translatedText.Contains("Invalid"))
                            lastMessage = translatedText;

                        // Switch new chat to the next placement.
                        ChatPlacementToggle();
                    }
                }
            }
        }
        #endregion

        #region Chat Timer

        // Display the text on the screen.
        private static void ChatTimer(object o)
        {
            // Display console in-game.
            if (Process.GetProcessesByName("Terraria").Length > 0)
            {
                // Get the games process pointer.
                IntPtr terrariaPtr = GetDC(IntPtr.Zero);

                // Get the windows position.
                // RECT wSize;
                // GetWindowRect(Process.GetProcessesByName("Terraria").Last().MainWindowHandle, out wSize);

                // Hide on-screen chat.
                if (!showChat)
                    return;

                // Change the chats by adding one.
                if (messageOffset == 0)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 300);
                    g.Dispose();
                }
                else if (messageOffset == 1)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 330);
                    g.Dispose();
                }
                else if (messageOffset == 2)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 360);
                    g.Dispose();
                }
                else if (messageOffset == 3)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 390);
                    g.Dispose();
                }
                else if (messageOffset == 4)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 420);
                    g.Dispose();
                }
                else if (messageOffset == 5)
                {
                    Graphics g = Graphics.FromHdc(terrariaPtr);
                    g.DrawString(lastMessage, new Font(FontFamily.GenericSerif, 24, FontStyle.Regular), Brushes.Red, 100, 450);
                    g.Dispose();
                }
                ReleaseDC(IntPtr.Zero, terrariaPtr);
            }
        }
        #endregion

        #region Basic Functions

        // Toggle the chats.
        private static void ChatPlacementToggle()
        {
            // Change the chats by adding one.
            if (messageOffset == 0)
            {
                messageOffset++;
            }
            else if (messageOffset == 1)
            {
                messageOffset++;
            }
            else if (messageOffset == 2)
            {
                messageOffset++;
            }
            else if (messageOffset == 3)
            {
                messageOffset++;
            }
            else if (messageOffset == 4)
            {
                messageOffset++;
            }
            else if (messageOffset == 5)
            {
                messageOffset = 0; // Reset.
            }
        }

        // Convert hex to string.
        public static byte[] FromHex(string hex)
        {
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        // Translate strings.
        public static String Translate(String word, string langFrom, string langTo)
        {
            try
            {
                var toLanguage = langTo; // To.
                var fromLanguage = langFrom; // From.
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";
                var webClient = new WebClient
                {
                    Encoding = System.Text.Encoding.UTF8
                };
                var result = webClient.DownloadString(url);
                try
                {
                    result = result[4..result.IndexOf("\"", 4, StringComparison.Ordinal)];
                    return result;
                }
                catch
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}