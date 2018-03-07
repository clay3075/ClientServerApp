using System;
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
            _server.AddCommand("ls", lsCmd);
            _server.AddCommand("cd", cdCmd);
        }

        private void HandleIncomingCalls()
        {
            _server.Listen();
            while (true)
            {
                var res = _server.ReadCommand().ToString();
                var ret = (object)"Failure";
                if (!string.IsNullOrWhiteSpace(res))
                {
                    string command = res;
                    string param = null;
                    if (res.Contains(' '))
                    {
                        param = res.Remove(0, res.IndexOf(' ') + 1);
                        var tmpIndex = res.IndexOf(' ');
                        command = tmpIndex > 0 ? res.Remove(tmpIndex) : null;
                    }
                    
                    ret = _server.HandleIncomingCommands(command?.Trim(), param?.Trim());
                }
                
                _server.SendResponse(ret);
            }
        }

        private object lsCmd(object obj)
        {
            var directories = Directory.GetDirectories(Directory.GetCurrentDirectory());
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());
            return directories.Concat(files).ToArray();
        }

        private object cdCmd(object obj)
        {
            Directory.SetCurrentDirectory((string) obj);
            return "Success";
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
