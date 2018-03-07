using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace libClient
{
    public class Client
    {
        private readonly TcpClient _client;

        public Client(string ip="127.0.0.1", int port=3000)
        {
            _client = new TcpClient(ip, port)
            {
                ReceiveBufferSize = 1024,
                SendBufferSize = 1024
            };
        }

        private NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public object ReadResponse()
        {
            var ret = new byte[0];
            int blockSize = 1024;
            Byte[] data = new byte[blockSize];

            do
            {
                GetStream().Read(data, 0, blockSize);
                ret = ret.Concat(data).ToArray();
                
            } while (GetStream().DataAvailable);

            return ByteArrayToObject(ret);
        }

        public void SendCommand(object command)
        {
            var buffer = ObjectToByteArray(command ?? "");
            GetStream().Write(buffer, 0, buffer.GetLength(0));
            GetStream().Flush();
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
