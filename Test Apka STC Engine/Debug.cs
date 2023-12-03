using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STCEngine
{
    public class Debug
    {
        public static void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            //Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Log(object messageObject)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(messageObject.ToString());
            //Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogError(object messageObject)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(messageObject.ToString());
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogWarning(object messageObject)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(messageObject.ToString());
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void LogInfo(object messageObject)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(messageObject.ToString());
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
