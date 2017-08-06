using System;

namespace MinerKiller
{
    class Logger
    {
        public static void Log(string text)
        {
            Console.WriteLine("[{0:hh:mm:ss.fff tt}]: {1}", DateTime.Now, text);
        }

        public static void LogError(string text)
        {
            Console.Write("[{0:hh:mm:ss.fff tt}]: ", DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void LogSuccess(string text)
        {
            Console.Write("[{0:hh:mm:ss.fff tt}]: ", DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void LogWarn(string text)
        {
            Console.Write("[{0:hh:mm:ss.fff tt}]: ", DateTime.Now);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
