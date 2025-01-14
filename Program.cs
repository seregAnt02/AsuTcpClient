using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using ChatClient.Video;
using System.Diagnostics;

namespace ChatClient
{
    class Program
    {        
        //====================================================
        static void Main(string[] args)
        {
            Socket socket = new Socket();
            socket.Start();
        }               
    }
}