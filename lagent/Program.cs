using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lagent
{
    class Program
    {
        static IPEndPoint ipeHome = null;
        private static IPEndPoint ipeLocalService = null;

        static bool connectedToLocalService = false;
        static bool tryConnectLocalService = false;

        static Socket socketHome;
        static Socket socketLocalService;

        public static void localServiceHandler()
        {
            byte[] buffer = new byte[1024];
            while (true)
                try
                {
                    socketLocalService = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                    while (!tryConnectLocalService)
                        Thread.Sleep(100);
                
                    socketLocalService.Connect(ipeLocalService);

                    Console.WriteLine("connected to local service {0}",
                        socketLocalService.RemoteEndPoint.ToString());

                    int bytesRec = 0;
                    try
                    {
                        while ((bytesRec = socketLocalService.Receive(buffer)) > 0)
                        {
                            Console.WriteLine("-H-> {0} bytes", bytesRec);
                            int bytesSent = socketHome.Send(buffer, bytesRec, SocketFlags.None);
                        }
                    } catch (Exception ex)
                    {
                        // ignore
                    }

                    tryConnectLocalService = false;

                    if (socketLocalService.Connected)
                    {
                        socketLocalService.Shutdown(SocketShutdown.Both);
                        socketLocalService.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
        }

        public static void connectHome()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                Console.Write("connecting home {0}: ",
                        ipeHome.ToString());
                while (true)
                {
                    
                    try
                    {
                        socketHome = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);


                        socketHome.Connect(ipeHome);
                        Console.WriteLine("connected.");

                        int bytesRec = 0;
                        while ((bytesRec = socketHome.Receive(buffer)) > 0)
                        {
                            if (!connectedToLocalService)
                                tryConnectLocalService = true;

                            while (!socketLocalService.Connected)
                                Thread.Sleep(100);

                            Console.WriteLine("<S-- {0} bytes", bytesRec);

                            int bytesSent = socketLocalService.Send(buffer, bytesRec, SocketFlags.None);
                        }
                        socketHome.Shutdown(SocketShutdown.Both);
                        socketHome.Close();
                    }
                    catch (Exception e)
                    {
                        Console.Write(".");
                    }
                }
                Thread.Sleep(1000);
            }
        }

        static void Main(string[] args)
        {

            if (args.Length >= 1)
            {
                try
                {
                    string[] s = args[0].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeLocalService = new IPEndPoint(iPAddress, p);
                }
                catch (Exception e)
                {
                    Console.WriteLine("could not parse local socket, using defaults");
                }
            }

            if (args.Length >= 2)
            {
                try
                {
                    string[] s = args[1].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeHome = new IPEndPoint(iPAddress, p);
                }
                catch (Exception e)
                {
                    Console.WriteLine("could not parse home socket, using defaults");
                }
            }

            Thread t = new Thread(new ThreadStart(localServiceHandler));
            t.Start();

            connectHome();
        }
    }
}
