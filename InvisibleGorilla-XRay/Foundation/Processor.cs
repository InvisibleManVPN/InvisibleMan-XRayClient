using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace InvisibleGorillaXRay.Foundation
{
    using Core;
    using Values;

    public class Processor
    {
        private Dictionary<string, Process> processes;

        public Processor()
        {
            this.processes = new Dictionary<string, Process>();
        }

        public void StartProcess(string processName, string fileName, string workingDirectory, string command, bool runAsAdmin)
        {
            try
            {
                DiagnosticLog.Write(
                    "Processor",
                    $"StartProcess requested: processName={processName}, fileName={fileName}, " +
                    $"workingDirectory={workingDirectory}, command={command}, runAsAdmin={runAsAdmin}");
                DiagnosticLog.Write(
                    "Processor",
                    $"StartProcess paths: fileExists={System.IO.File.Exists(fileName)}, " +
                    $"workingDirectoryExists={System.IO.Directory.Exists(workingDirectory)}");

                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.WorkingDirectory = workingDirectory;

                if (runAsAdmin)
                    process.StartInfo.Verb = "runas";
                
                process.Start();
                AddProcess(process, processName);
                DiagnosticLog.Write("Processor", $"Process started: processName={processName}, pid={process.Id}");
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Processor.StartProcess", ex);
                Debug.WriteLine($"Failed to start process '{processName}': {ex.Message}");
                StopProcess(processName);
            }
        }

        public void StopProcess(string processName)
        {
            if (!processes.TryGetValue(processName, out Process process))
                return;

            try
            {
                DiagnosticLog.Write("Processor", $"StopProcess: processName={processName}, pid={process.Id}");
                RemoveProcess(processName);
                process.Kill(true);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Processor.StopProcess", ex);
                Debug.WriteLine($"Failed to stop process '{processName}': {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        public void StopSystemProcesses(string processName)
        {
            Process[] runningProcesses = Process.GetProcessesByName(processName);

            foreach (Process process in runningProcesses)
            {
                try
                {
                    DiagnosticLog.Write("Processor", $"StopSystemProcesses killing: processName={processName}, pid={process.Id}");
                    process.Kill();
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException("Processor.StopSystemProcesses", ex);
                    Debug.WriteLine($"Failed to kill system process '{processName}': {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private void AddProcess(Process process, string processName)
        {
            if (processes.ContainsKey(processName))
            {
                processes[processName].Dispose();
                processes.Remove(processName);
            }
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
            if (!processes.TryGetValue(processName, out Process process))
                return false;
            
            try
            {
                if (process.HasExited)
                {
                    RemoveProcess(processName);
                    process.Dispose();
                    return false;
                }
                
                return Process.GetProcessById(process.Id) != null;
            }
            catch
            {
                RemoveProcess(processName);
                return false;
            }
        }

        private bool IsProcessExists(string processName) => processes.ContainsKey(processName);
    }
}
