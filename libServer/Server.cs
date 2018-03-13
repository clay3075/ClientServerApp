using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace libServer
{
    public class Server
    {
        private readonly TcpListener _connection;
        private readonly Dictionary<string, Func<object[], object>> _commands = new Dictionary<string, Func<object[], object>>();
        private Dictionary<string, string> _documentation = new Dictionary<string, string>();
        private Socket _client;

        public Server()
        {
            _connection = new TcpListener(IPAddress.Parse("127.0.0.1"), 3000);
            _connection.Start();
        }

        public void Listen()
        {
            _client = _connection.AcceptSocket();
        }

        public object HandleIncomingCommands(string currCommand, object[] param=null)
        {
            return (from command in _commands.Keys where command == currCommand select _commands[command](param)).FirstOrDefault();
        }

        public void AddCommand(string command, Func<object[], object> action, string description = "No description.")
        {
            _commands.Add(command, action);
            _documentation.Add(command, description);
        }

        public string GetDocumentation()
        {
            return string.Join("\n", from document in _documentation select document.Key + ": " + document.Value);
        }

        private NetworkStream GetClientStream()
        {
            return new NetworkStream(_client);
        }

        public object ReadCommand()
        {
            var ret = new byte[0];
            int blockSize = 1024;
            Byte[] data = new byte[blockSize];

            do
            {
                GetClientStream().Read(data, 0, blockSize);
                ret = ret.Concat(data).ToArray();

            } while (GetClientStream().DataAvailable);

            return ByteArrayToObject(ret);
        }

        public void SendResponse(object res)
        {
            var buffer = ObjectToByteArray(res ?? "");
            GetClientStream().Write(buffer, 0, buffer.GetLength(0));
            GetClientStream().Flush();
        }

        public void CloseConnection()
        {
            _client.Close();
        }

        // Convert an object to a byte array
        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}
