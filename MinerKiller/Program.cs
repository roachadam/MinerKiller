using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MinerKiller
{
    class Program
    {
        private static int[] _PortList = new[]
        {
            9999,
            14444,
            14433,
            6666,
            16666,
            6633,
            16633,
            4444,
            14444,
            3333,
            13333,
            7777,
            5555
        };

        private static string[] _Nvidia = new[]
        {
            "nvcompiler.dll",
            "nvopencl.dll",
            "nvfatbinaryLoader.dll",
            "nvapi64.dll",
            "OpenCL.dll"
        };

        private static string[] _Amd = new[] { ""};
        //todo: amd libraries
        static void Main(string[] args)
        {
            WaterMark();
            
            Logger.Log("Scanning started...");

            var cons = GetConnections();
            

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    ProcessModule t = p.Modules[0];
                }
                catch (Exception){
                    continue;           
                }

                int modCount = 0;
                foreach (ProcessModule pMod in p.Modules)
                {
                    foreach(string name in _Nvidia)
                        if (pMod.ModuleName.ToLower().Equals(name.ToLower()))
                            modCount++;   
                }
                if (modCount > 2)
                {
                    Logger.Log("Miner Found - Process: " + p.ProcessName + ".exe, ProcessId: " + p.Id);
                    var tiedConnections = cons.Where(x => x.ProcessId == p.Id);
                    var badPorts = tiedConnections.Where(x => _PortList.Any(y => y == x.RemotePort));
                    foreach (var conn in badPorts)
                    {
                        Logger.LogSuccess("\t" + conn);
                    }
                    p.Kill();
                }
            }
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
        // Adapted from
        // http://www.cheynewallace.com/get-active-ports-and-associated-process-names-in-c/
        public static List<Connection> GetConnections()
        {
            var Connections = new List<Connection>();

            try
            {
                using (Process p = new Process())
                {

                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.Arguments = "-a -n -o";
                    ps.FileName = "netstat.exe";
                    ps.UseShellExecute = false;
                    ps.WindowStyle = ProcessWindowStyle.Hidden;
                    ps.RedirectStandardInput = true;
                    ps.RedirectStandardOutput = true;
                    ps.RedirectStandardError = true;

                    p.StartInfo = ps;
                    p.Start();

                    StreamReader stdOutput = p.StandardOutput;
                    StreamReader stdError = p.StandardError;

                    string content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                    string exitStatus = p.ExitCode.ToString();

                    if (exitStatus != "0")
                    {
                        Logger.LogError("error reading tcp connections");
                        return null;
                    }

                    string[] rows = Regex.Split(content, "\r\n");
                    foreach (string row in rows)
                    {
                        if (string.IsNullOrEmpty(row)) continue;

                        if (row.Contains("0.0.0.0") || row.Contains("127.0.0.1") || row.Contains("[::")) continue;
                        string[] tokens = Regex.Split(row, "\\s+");
                        if (tokens.Length > 4 &&  tokens[1].Equals("TCP"))
                        {
                            string t = tokens[3].Split(':')[1];
                            int remotePort = int.Parse(t);
                            Connections.Add(new Connection()
                            {
                                ProcessId = int.Parse(tokens[5]),
                                RemotePort = remotePort,
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Connections;
        }

        public static string LookupProcess(int pid)
        {
            string procName;
            try { procName = Process.GetProcessById(pid).ProcessName; }
            catch (Exception) { procName = "-"; }
            return procName;
        }

        public class Connection
        {

            public int RemotePort { get; set; }
            public int ProcessId{ get; set; }

            public override string ToString()
            {
                return "TCP Connection - Process Id: " + ProcessId + ", Port: " + RemotePort;
            }

        }
    }
}
