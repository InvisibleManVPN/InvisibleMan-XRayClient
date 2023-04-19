using System.Diagnostics;
using System.Collections.Generic;

namespace InvisibleManXRay.Foundation
{
    using Values;

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
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.WorkingDirectory = Directory.SERVICES;

                if (runAsAdmin)
                    process.StartInfo.Verb = "runas";
                
                process.Start();
                AddProcess(process, processName);
            }
            catch
            {
                StopProcess(processName);
            }
        }

        public void StopProcess(string processName)
        {
            try
            {
                Process process = processes[processName];
                RemoveProcess(processName);
                process.Kill(true);
            }
            catch
            {
                
            }
        }

        public void StopSystemProcesses(string processName)
        {
            Process[] runningProcesses = Process.GetProcessesByName(processName);

            foreach(Process process in runningProcesses)
            {
                process.Kill();
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

        public bool IsProcessRunning(string processName) 
        {
            if (!IsProcessExists(processName))
                return false;
            
            if (processes[processName].HasExited)
            {
                RemoveProcess(processName);
                return false;
            }
            
            return Process.GetProcessById(processes[processName].Id) != null;
        }

        private bool IsProcessExists(string processName) => processes.ContainsKey(processName);
    }
}