using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MinerKiller
{
    class MinerKiller
    {
        private int[] _PortList = new[]
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
            5555,
            9980
        };

        private string[] _Nvidia = new[]
        {
            "nvcompiler.dll",
            "nvopencl.dll",
            "nvfatbinaryLoader.dll",
            "nvapi64.dll",
            "OpenCL.dll"
        };

        private string[] _Amd = new[] { "" };

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        public void Scan()
        {
            // Get processes with active TCP connections
            var cons = GetConnections();

            foreach (Process p in GetProcesses())
            {
                Logger.Log("Scanning process: " + p.ProcessName + ".exe");


                double riskLevel = 0; // Number to describe the likelihood a process being a miner.
                int modCount = 0;
                foreach (ProcessModule pMod in p.Modules)
                {
                    foreach (string name in _Nvidia)
                        if (pMod.ModuleName.ToLower().Equals(name.ToLower()))
                            modCount++;
                }


                // If process has loaded more than 2 GPU libraries, increave level
                if (modCount > 2)
                {
                    Logger.LogWarn("Potential Miner Found - Process: " + p.ProcessName + ".exe, ProcessId: " + p.Id);
                    riskLevel += 2;
                }
                else
                {
                    continue; // todo: determine if this should be the "all" factor
                }


                // Check tcp connections associated with suspect process
                var tiedConnections = cons.Where(x => x.ProcessId == p.Id);
                var badPorts = tiedConnections.Where(x => _PortList.Any(y => y == x.RemotePort));
                foreach (var conn in badPorts)
                {
                    Logger.Log("\t" + conn);
                    riskLevel += 1;
                }


                // Check commandline arguments of suspect process
                var args = GetCommandLine(p);

                // If commandline contains blacklisted/active tcp port, increase level by 1/2
                if (args != null)
                {
                    foreach (var port in _PortList)
                    {
                        bool portActive = badPorts.Any(x => x.RemotePort == port);
                        if (portActive && args.Contains(port.ToString()))
                        {
                            riskLevel += 2;
                            Logger.Log("\tBlacklisted Active Port in CMD Args: " + port);
                        }
                        else if (args.Contains(port.ToString()))
                        {
                            riskLevel += 1;
                            Logger.Log("\tBlacklisted Port in CMD Args: " + port);
                        }
                    }
                    if (args.Contains("pool"))
                    {
                        riskLevel += .5;
                        Logger.Log("\t\"Pool\" in CMD Args.");
                    }
                    // checks if cmdline cointains "YOUR_ADDRESS.YOUR_WORKER_NAME/EMAIL" format
                    // TODO: HORRIBLE REGEX PLEASE LEARN IT AND FIX
                    var match = Regex.Match(args, @"\w+\.\w+\/\w+\@\w+\.\w+");

                    if (match.Success)
                    {
                        riskLevel += 1;
                        Logger.Log("\tConfig Data in CMD Args: " + match.Value);
                    }
                }


                // TODO: needs work, this isn't acurate
                if (p.MainWindowHandle == IntPtr.Zero )
                {
                    riskLevel += 2;
                    Logger.Log("\tProcess Window is hidden.");
                }
                
                if (riskLevel >= 3.5)
                {
                    Logger.Log("\tRisk Level: " + riskLevel);
                    try
                    {
                        p.Kill();
                        if(p.HasExited)
                            Logger.LogSuccess("Successfully killed Silent Miner: " + p.ProcessName + ".exe!");
                        else
                        {
                            throw new Exception("Process killing failed.");
                            
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Failed to kill Silent Miner: " + p.ProcessName + ".exe!\r\n" + e);
                    }
                }

            }
        }

        private List<Process> GetProcesses()
        {
            List<Process> procs = new List<Process>();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    // Trycatch here to weed out non-accessable processes
                    ProcessModule t = p.Modules[0];
                }
                catch (Exception)
                {
                    continue;
                }
                procs.Add(p);
            }
            return procs;
        }


        #region " TCP Connections "
        // Adapted from
        // http://www.cheynewallace.com/get-active-ports-and-associated-process-names-in-c/
        private List<Connection> GetConnections()
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
                        Logger.LogError("Failed reading TCP connections.");
                        return null;
                    }

                    string[] rows = Regex.Split(content, "\r\n");
                    foreach (string row in rows)
                    {
                        if (String.IsNullOrEmpty(row)) continue;

                        if (row.Contains("0.0.0.0") || row.Contains("127.0.0.1") || row.Contains("[::")) continue;
                        string[] tokens = Regex.Split(row, "\\s+");
                        if (tokens.Length > 4 && tokens[1].Equals("TCP"))
                        {
                            string t = tokens[3].Split(':')[1];
                            int remotePort = Int32.Parse(t);
                            Connections.Add(new Connection()
                            {
                                ProcessId = Int32.Parse(tokens[5]),
                                RemotePort = remotePort,
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
            return Connections;
        }

        public class Connection
        {
            public int RemotePort { get; set; }
            public int ProcessId { get; set; }

            public override string ToString()
            {
                return "TCP Connection - Process Id: " + ProcessId + ", Port: " + RemotePort;
            }
        }
        #endregion

        #region " Commandline Args "

        private string GetCommandLine(Process process)
        {
            string cmdLine = null;
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            {
                var matchEnum = searcher.Get().GetEnumerator();
                if (matchEnum.MoveNext())
                {
                    cmdLine = matchEnum.Current["CommandLine"]?.ToString();
                }
            }
            return cmdLine;
        }

        #endregion
    }
}
