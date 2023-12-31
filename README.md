# Welcome To The TerraTranslate Project!
This project is aimed at translating public servers and to allow you to join in on the communication. Have you ever joined an online server and found everyone is communicating in a different language such as Spanish, Russian, or Chinese? This is frustrating is it not? Well then TerraTranslate is the tool for you! This tool is 100% external and requires no installation into your game files or special mod loaders!

![T-Translate](https://github.com/RussDev7/TerraTranslate/assets/33048298/2fc71d06-3e48-43a3-8f9c-5ef30dc800bd)


## How To Use It?
There are currently only four easy steps required to use this tool.
1) Join a server with Terraria.
2) Launch TerraTranslate.
3) Select your network device.

4) Choose languages to translate.

The console will display all translated messages in simple text. TerraTranslate also saves a text log of all messages to your desktop named `TranslatedServerLog.txt`. This log contains a more advanced logging including the timestamp, data, length and hex of each message received.

## Supported In-Game Commands
- `/lang {text}` - Translates a message to the opposing language and copies it to the clipboard.
- `/show` - Shows the translated chat in-game.
- `/hide` - Hides the in-game text translation overlay.

![In-Game](https://github.com/RussDev7/TerraTranslate/assets/33048298/99f1d2d8-d91c-43ba-8fec-ccf09704f806)

## Currently Supported Languages
- ZH <-> EN (Chinese to English / English to Chinese)
- RU <-> EN (Russian to English / English to Russian)
- ES <-> EN (Spanish to English / English to Spanish)

For a full list of possible languages that could be implemented, please refer to the [GoogleLanguageSupport](https://cloud.google.com/translate/docs/languages).

## How It Works
This project works by using a packet sniffer to read the network using the filter `tcp dst port GAMESPORT`. This gives the raw binary packet recieved from the Terraria server in the form of hex. We can convert this to usable UTF8 text.

![T-Packet1](https://github.com/RussDev7/TerraTranslate/assets/33048298/14e76274-ce8e-4a8c-834e-808d29ed1f1c)

To dynamically capture the port the game uses I created an already published API class called [TcpConnectionInfo.cs](https://github.com/RussDev7/GetPortsFromProcessName).
This allows us to gather the following from established window processes:
+ `TryGetLocalPort()` - Finds the local port.
+ `TryGetRemotePort()` - Finds the remote port.

From there we can inspect this incoming packet and extract only the in-game text data. Allot of garbage is in each packet we don’t need. The structure of Terraria packets contains a unique delimitator at the start of a message text. To find this, we can capture a bunch of packets from multiple servers to find an array of bytes that is unique. In my research I found `00520100` to be unique for this.

![T-Packet2](https://github.com/RussDev7/TerraTranslate/assets/33048298/f3fb09de-46d0-4e41-9f2f-eea65859e285)
Then we can run the code through googles translate API for translating before sending it to the console and recording it. Its also possible to alter the packet and send it in-game to it appears translated in-game. Future plans I would also like to capture in-game commands such as `/trans {msg}` or `/lang` to have the console translate to the opposing language or switch languages.

```csharp
public static String Translate(String word, string langFrom, string langTo)
{
	var toLanguage = langTo; // To.
	var fromLanguage = langFrom; // From.
	var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=
 		{fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";
	var webClient = new WebClient{Encoding = System.Text.Encoding.UTF8};
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
```
- Code by: [Yashar Aliabbasi](https://stackoverflow.com/a/52604936/8667430)

## Sharppcap
The foundation for this project was created on a free templet within the [sharppcap](https://github.com/dotpcap/sharppcap) repository. Huge thanks to the sharppcap team and all their contributors. I take no credit for their amazing work.

## Project Roadmap
 - [x] Dynamically get the local port of terraria.
 - [ ] Dynamically set the capture device from a .json.
 - [x] Dynamically capture only destination traffic over this port.
 - [x] Create a secondary filter to read traffic and collect only chat. (Use chat headers)
 - [x] Translate incoming packets.
 - [ ] Use the console to translate text to the opposing language. (Copies to clipboard)
 - [x] Capture incoming game packets (Ex: /trans /lang)
 - [ ] Dynamically set the translation languages from a .json.
 - [x] Send the altered packet out to be dislayed within the game.

## Required Libraries
- [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [WinPcap 4.1.3](https://www.winpcap.org/install/)
