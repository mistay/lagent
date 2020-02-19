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
        private static void Receive(Socket client)
        {
           
        }
        private static void ReceiveLocalCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                int received = state.workSocket.EndReceive(ar);
                state.workSocket
                    .BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveLocalCallback), state);

                if (received > 0)
                {
                    Console.WriteLine("sending {0} bytes home", received);
                    socketHome.Send(state.buffer, received, SocketFlags.None);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static bool connectedHome=false;

        private static void ReceiveHomeCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                int received = state.workSocket.EndReceive(ar);
                state.workSocket
                    .BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveHomeCallback), state);

                if (received > 0)
                {
                    Console.WriteLine("sending {0} bytes to agent", received);

                    if (!connectedHome)
                    {
                        connectLocally(ipeLocal);
                        connectedHome = true;
                    }


                    socketLocal.Send(state.buffer, received, SocketFlags.None);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static Socket socketHome = null;
        static Socket socketLocal = null;

        static IPEndPoint ipeLocal;

        static void Main(string[] args)
        {

            Console.WriteLine("args, len: " + args.Length);
            for (int i=0; i<args.Length; i++)
            {
                Console.WriteLine("args[{0}]: {1}", i, args[i]);
            }

            ipeLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3389);
            IPEndPoint ipeHome = new IPEndPoint(IPAddress.Parse("10.100.1.254"), 11000);

            if (args.Length >= 1)
            {
                try
                {
                    string[] s = args[0].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeLocal = new IPEndPoint(iPAddress, p);
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

            connectHome(ipeHome);


            

            try
            {
                Console.WriteLine("press any key to exit...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.Write("e: " + e.ToString());
            }
        }

        private static void connectLocally(IPEndPoint ipe)
        {
            try
            {
                socketLocal = new Socket(SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("Connecting to local service: {0}:{1}", ipe.Address.ToString(), ipe.Port);

                socketLocal.Connect(ipe);
                Console.WriteLine("connecting RDP done");
            }
            catch (ArgumentNullException ae)
            {
                Console.WriteLine("ArgumentNullException : {0}", ae.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
            try
            {

                StateObject state = new StateObject();
                state.workSocket = socketLocal;

                // Begin receiving the data from the remote device.  
                socketLocal.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveLocalCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void connectHome(IPEndPoint ipe)
        {
            Console.Write("calling home: {0}:{1}", ipe.Address.ToString(), ipe.Port);
            socketHome = new Socket(SocketType.Stream, ProtocolType.Tcp);

            while (true)
            {
                try
                {
                    socketHome.Connect(ipe);
                    Console.WriteLine(" connection established successfully.");
                    break;
                }
                catch (Exception e)
                {
                }
                Console.Write(".");
                Thread.Sleep(3);
            }

            try
            {
                StateObject state = new StateObject();
                state.workSocket = socketHome;

                // Begin receiving the data from the remote device.  
                socketHome.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveHomeCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
