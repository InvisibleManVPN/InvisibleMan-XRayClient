using System;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace InvisibleManXRay.Foundation
{
    public class Processor
    {
        private Dictionary<string, Process> processes;

        public Processor()
        {
            this.processes = new Dictionary<string, Process>();
        }

        public void StartProcess(string processName, string fileName, string command, bool runAsAdmin)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                if (runAsAdmin)
                    process.StartInfo.Verb = "runas";
                
                process.Start();
                AddProcess(process, processName);

                process.WaitForExit();
                RemoveProcess(processName);
            }
            catch
            {
                StopProcess(processName);
            }
        }

        public void StartProcessAsThread(string processName, string fileName, string command, bool runAsAdmin)
        {
            new Thread(() => {
                StartProcess(processName, fileName, command, runAsAdmin);
            }).Start();
        }

        public void StopProcess(string processName)
        {
            try
            {
                Process process = processes[processName];
                RemoveProcess(processName);
                process.Kill(true);

                Process tun2SocksProcess = Process.GetProcessesByName("tun2socks").FirstOrDefault();
                tun2SocksProcess.Kill();
            }
            catch
            {
                
            }
        }

        public void StopProcessAndChildren(string processName)
        {
            try
            {
                int processId = processes[processName].Id;
                StopProcess(processName);
                KillProcessAndChildren(processId);
            }
            catch
            {

            }

            void KillProcessAndChildren(int processId)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    queryString: $"select * from Win32_Process where ParentProcessID={processId}"
                );

                foreach(ManagementObject managementObject in searcher.Get())
                {
                    KillProcessAndChildren(Convert.ToInt32(managementObject["ProcessID"]));
                }

                try
                {
                    Process.GetProcessById(processId).Kill();
                }
                catch
                {

                }
            }
        }

        private void AddProcess(Process process, string processName)
        {
            processes.Add(processName, process);
        }

        private void RemoveProcess(string processName)
        {
            if (!IsProcessExists(processName))
                return;
            
            processes.Remove(processName);
        }

        private bool IsProcessExists(string processName) => processes.ContainsKey(processName);
    }
}