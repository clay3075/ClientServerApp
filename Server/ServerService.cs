﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Resources;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public partial class ServerService : ServiceBase
    {
        private readonly libServer.Server _server;
        public ServerService()
        {
            InitializeComponent();
            _server = new libServer.Server();
        }

        protected override void OnStart(string[] args)
        {
            var thread = new Thread(() =>
            {
                SetupCommands();
                HandleIncomingCalls();
            });
            thread.Start();
        }

        private void SetupCommands()
        {
            _server.AddCommand("ls", lsCmd, "Get list of all directories and files in current directory.");
            _server.AddCommand("cd", cdCmd, "Change current directory (ex. cd <directory_name>)");
            _server.AddCommand("stopservice", stopServiceCmd, "Stop windows service (ex. stopservice <service_name>)");
            _server.AddCommand("startservice", startServiceCmd, "Start windows service (ex. startservice <service_name>)");
            _server.AddCommand("servicestatus", serviceStatusCmd, "Get the current running status of the service (ex. servicestatus <service_name>)");
            _server.AddCommand("help", helpCmd, "This command will provide the user with a list of available commands.");
            _server.AddCommand("getfile", getFileCmd, "Used to download a file from the server. (ex. getfile <file_name>)");
            _server.AddCommand("sendfile", sendFileCmd, "Used to send a file to the server. (ex. sendfile <file_to_send> <what_to_name_file>");
            _server.AddCommand("tasklist", taskListCmd, "Used to return a list of all running processes on the server");
            _server.AddCommand("taskkillpid", taskKillPid, "Used to kill a task by its pid (ex. taskkillpid <pid>)");
            _server.AddCommand("taskkillname", taskKillName, "Used to kill a task by its name (ex. taskkillpid <name>)");
            _server.AddCommand("programstart", programStart, "Used to start a program. (ex. programstart <program_path>");
        }

        private void HandleIncomingCalls()
        {
            _server.Listen();
            try
            {
                while (true)
                {
                    var res = _server.ReadCommand().ToString();
                    var ret = (object) "Failure";
                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        var command = res;
                        object[] param = null;
                        var inputs = command.Split(' ');
                        if (inputs.Length > 1)
                        {
                            command = inputs[0];
                            param = (from input in inputs where input != command select input).ToArray();
                        }

                        ret = _server.HandleIncomingCommands(command?.Trim(), param);
                    }

                    _server.SendResponse(ret);
                }
            }
            catch (Exception e)
            {
                HandleIncomingCalls();
            }
            
        }

        private object lsCmd(object obj)
        {
            var directories = Directory.GetDirectories(Directory.GetCurrentDirectory());
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());
            return string.Join("\n", directories.Concat(files));
        }

        private object cdCmd(object[] obj)
        {
            var ret = "Success";
            try
            {
                Directory.SetCurrentDirectory(string.Join(" ", obj));
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            
            return ret;
        }

        private object stopServiceCmd(object[] obj)
        {
            var ret = "Success";
            try
            {
                var service = new ServiceController((string) obj[0]);
                service.Stop();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object helpCmd(object[] obj)
        {
            return _server.GetDocumentation();
        }

        private object startServiceCmd(object[] obj)
        {
            var ret = "Success";
            try
            {
                var service = new ServiceController((string)obj[0]);
                service.Start();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object serviceStatusCmd(object[] obj)
        {
            string ret;
            try
            {
                var service = new ServiceController((string)obj[0]);
                ret = service.Status.ToString();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object getFileCmd(object[] obj)
        {
            object[] retPair = new object[2];
            try
            {
                using (var file = File.OpenRead((string)obj[0]))
                {
                    retPair[1] = new byte[file.Length];
                    retPair[0] = file.Read((byte[])retPair[1], 0, (int)file.Length);
                }  
            }
            catch (Exception e)
            {
                retPair[0] = libServer.Server.ObjectToByteArray(e.Message);
            }

            return retPair;
        }

        private object sendFileCmd(object[] obj)
        {
            var ret = "Success";
            try
            {
                _server.SendResponse("Ready");
                var fileRes = (object[])_server.ReadCommand();
                if (fileRes[0] is int fileLength && fileRes[1] is byte[] fileContents && fileRes[2] is string fileName)
                {
                    using (var file = File.Create(fileName))
                    {
                        file.Write(fileContents, 0, fileLength);
                    }
                }
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object taskListCmd(object[] obj)
        {
            const int namePadding = 30;
            const int pidPadding = 10;
            const int sessionPadding = 5;
            const int memPadding = 15;
            Func<string, int, string> truncate = (string input, int max) => input?.Substring(0, Math.Min(input.Length, max));
            Func<string> createHeader = () => $"{"Image Name", -namePadding} {"PID", pidPadding} {"Session ID", -sessionPadding} {"Mem Usage", memPadding}\n" + $"{new string('=', namePadding)} {new string('=', pidPadding)} {new string('=', sessionPadding)} {new string('=', memPadding)}\n";
            Func<Process, string> formatProcess = (Process process) =>
            {
                var name = truncate(process.ProcessName, namePadding);
                var pid = truncate(process.Id.ToString(), pidPadding);
                var session = truncate(process.SessionId.ToString(), sessionPadding);
                var memory = truncate((process.PagedMemorySize64 / 1000).ToString() + " K", memPadding);
                return $"{name,-30} {pid,10} {session,-20} {memory,15}";
            };

            string ret;
            try
            {
                ret = createHeader() + string.Join("\n", from process in Process.GetProcesses() select formatProcess(process));
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object taskKillPid(object[] obj)
        {
            var ret = "Success";
            try
            {
                var pid = (int)obj[0];
                Process.GetProcessById(pid).Kill();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object programStart(object[] obj)
        {
            var ret = "Success";
            try
            {
                var programName = string.Join(" ", obj);
                Process.Start(Path.Combine(Environment.CurrentDirectory, programName));
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private object taskKillName(object[] obj)
        {
            var ret = "Success";
            try
            {
                var name = string.Join(" ", obj);
                foreach (var process in Process.GetProcessesByName(name)) process.Kill();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;
        }

        private void CloseConnections()
        {
            _server?.CloseConnection();
        }

        protected override void OnStop()
        {
            CloseConnections();
        }
    }
}
