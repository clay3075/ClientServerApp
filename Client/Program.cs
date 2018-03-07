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
                    switch (input)
                    {
                        case "ls":
                            if (response == null) goto default;
                            foreach (var path in (string[])response)
                                Console.WriteLine(path);
                            break;
                        case "cd":
                            if (response == null) goto default;
                            break;
                        case "Success":
                            Console.WriteLine("Success");
                            break;
                        default:
                            if ((string)response == "Success") goto case "Success";
                            Console.WriteLine("Failed");
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
