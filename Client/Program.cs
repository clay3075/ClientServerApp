using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("> Creating connection..");
            libClient.Client client = new libClient.Client();
            Console.WriteLine("> Connected.");
            string input;
            object response;
            do
            {
                Console.WriteLine("> Enter Command");
                Console.Write("$ ");
                input = Console.ReadLine();
                client.SendCommand(input);
                response = client.ReadResponse();
                try
                {
                    switch (input?.Split(' ')[0])
                    {
                        case "getfile":
                            var temp = (object[])response;
                            if (temp[0] is int fileLength && temp[1] is byte[] fileContents)
                            {
                                using (var file = File.Create(input.Split(' ')[2]))
                                {
                                    file.Write(fileContents, 0, fileLength);
                                }
                                Console.WriteLine("File retrieved.");
                            }
                            else
                            {
                                response = temp[0];
                                goto default;
                            }
                            break;
                        case "sendfile":
                            if ((string)response == "Ready")
                            {
                                var fileRes = new object[3];
                                using (var file = File.OpenRead(input.Split(' ')[1]))
                                {
                                    fileRes[1] = new byte[file.Length];
                                    fileRes[0] = file.Read((byte[]) fileRes[1], 0, (int)file.Length);
                                }
                                fileRes[2] = input.Split(' ')[2];
                                client.SendCommand(fileRes);
                                response = "Success";
                                goto default;
                            }
                            else
                            {
                                goto default;
                            }
                        default:
                            Console.WriteLine(response);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            } while (input?.Trim().ToLower() != "exit");
        }
    }
}
