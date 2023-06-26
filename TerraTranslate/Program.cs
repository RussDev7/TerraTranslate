using System;
using System.Text;
using SharpPcap;
using System.IO;
using System.Net;
using System.Web;
using System.Linq;

namespace Example5
{
    public class Program
    {
        public static string tanslateFromLanguage = "zh"; // Chinese
        public static string tanslateToLanguage = "en"; // English
        public static void Main()
        {
            // Print SharpPcap version
            // var ver = Pcap.SharpPcapVersion;

            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("ERROR: No devices were found on this machine");
                return;
            }

            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            int i = 0;

            // Scan the list printing every entry
            foreach (var dev in devices)
            {
                Console.WriteLine("{0}) {1}", i, dev.Description);
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");
            i = int.Parse(Console.ReadLine());

            using var device = devices[i];

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
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            //Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);

            // Get the local port terraria is running on.
            ushort terrariaPort = TcpConnectionInfo.TryGetLocalPort("Terraria", out ushort value);
            if (terrariaPort == 0)
            {
                Console.WriteLine("ERROR: Game is currently not running.");
                return;
            }

            // tcpdump filter to capture only TCP/IP packets
            string filter = "tcp dst port " + terrariaPort;
            device.Filter = filter;

            // Send start message to console.
            Console.Clear();
            Console.WriteLine("-- TerraTranslate v1.0 by discord:dannyruss is now starting...");
            Console.WriteLine
                ("-- The following tcpdump filter will be applied: \"{0}\"",
                filter);
            Console.WriteLine
                ("-- Listening on {0}, hit 'Ctrl-C' to exit...",
                device.Description);
            Console.WriteLine("----------------------------------------------------");

            // Write new entree to file.
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\TranslatedServerLog.txt", "[*] Joining a new server! Eastablishing a new connection: " + filter + Environment.NewLine);

            // Start capture packets
            device.Capture();
        }

        /// <summary>
        /// Prints the time and length of each received packet
        /// </summary>

        // Get packet.
        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            // Filter only chat messages.
            if (Convert.ToHexString(e.Data).Contains("00520100"))
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

                // Convert message text to UTF8 and trim.
                var messageTextUTF8 = Encoding.UTF8.GetString(FromHex(messageLastHalf)).Substring(3)[..^3];

                // Do Translate stuff.
                var translatedText = Translate(messageTextUTF8, tanslateFromLanguage, tanslateToLanguage);

                // Convert string back to original.
                // var messageTranslatedHex = messageFirstHalf + BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(translatedText)).Replace("-", "");
                // var messageTranslatedByte = FromHex(messageTranslatedHex);

                // Display message.
                File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\TranslatedServerLog.txt", "Time=" + time.Hour + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond + ", Data=" + translatedText + ", Lengh=" + lengh + ", Hex: " + messageLastHalf + Environment.NewLine);
                Console.WriteLine(translatedText);

                // Send back modified packet.
            }
        }

        // Convert hex to 
        public static byte[] FromHex(string hex)
        {
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        // Translate string to english
        public static String Translate(String word, string langFrom, string langTo)
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
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                return result;
            }
            catch
            {
                return "Error";
            }
        }
    }
}
