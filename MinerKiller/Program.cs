using System;
using System.Diagnostics;

namespace MinerKiller
{
    class Program
    {
        static void Main(string[] args)
        {
            WaterMark();

            MinerKiller mk = new MinerKiller();

            Logger.Log("Scanning started...");

            mk.Scan();
            
            Logger.Log("Finished.");

            Console.Read();
        }

        private static void WaterMark()
        {
            Console.WriteLine(@"    __  ____                 __ __ _ ____         ");
            Console.WriteLine(@"   /  |/  (_)___  ___  _____/ //_/(_) / /__  _____");
            Console.WriteLine(@"  / /|_/ / / __ \/ _ \/ ___/ ,<  / / / / _ \/ ___/");
            Console.WriteLine(@" / /  / / / / / /  __/ /  / /| |/ / / /  __/ /    ");
            Console.WriteLine(@"/_/  /_/_/_/ /_/\___/_/  /_/ |_/_/_/_/\___/_/     ");
            Console.WriteLine("\t\tby: Adam Roach\r\n");
        }

    }
}
