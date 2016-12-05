using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharp;
using System.Reflection;
using System.IO;

namespace DiscordSharpTest
{
    class Program
    {
        //http://stackoverflow.com/questions/1600962/displaying-the-build-date
        public static DateTime GetLinkerTime(TimeZoneInfo target = null)
        {
            var assembly = Assembly.GetEntryAssembly();
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        static void Main(string[] args)
        {
            Console.Title = GetLinkerTime().ToString();

            WubbyBot bot = new WubbyBot("wubbybot", "log-wubby");
            bot.Login();
            bot.Init();

            /*if ((bot.Connect()) != null)
                Console.WriteLine(string.Format("{0} successfully connected.", bot._name));
            else
                Console.WriteLine(string.Format("{0} failed to connect.", bot._name));*/
            //if (client.SendLoginRequest() != null)
            //    ClientTask(client);
            Console.ReadLine();
            //bot.Client.GetTextClientLogger.Save(@"C:\Users\Sam\Desktop\discord.txt");

            bot.Shutdown();

        }
    }
}
