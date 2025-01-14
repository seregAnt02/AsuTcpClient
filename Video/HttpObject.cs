using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ChatClient.Video
{
    class HttpObject
    {
        //------------------------------------------------           
        public void TcpRequest(string server, int port)
        {
            Task<byte[]> rezult = HttpRequestAutentication();
            if (rezult != null) rezult.Wait();
            //Tcp_client(server, port);
        }
        //------------------------------------------------        
        private void Tcp_client(string server, int port)
        {
            try
            {
                using (TcpClient tcp_client = new TcpClient())
                {
                    tcp_client.Connect(server, port);

                    byte[] data = new byte[256];
                    StringBuilder response = new StringBuilder();
                    using (NetworkStream stream = tcp_client.GetStream())
                    {
                        do
                        {
                            int bytes = stream.Read(data, 0, data.Length);
                            response.Append(Encoding.UTF8.GetString(data, 0, bytes));
                        }
                        while (stream.DataAvailable); // пока данные есть в потоке

                        Console.WriteLine(response.ToString());

                        // Закрываем потоки
                        stream.Close();
                    }
                    tcp_client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            Console.WriteLine("Запрос завершен...");
            Console.Read();
        }     
        //------------------------------------------------
        private async Task<byte[]> HttpRequestAutentication()
        {
            byte[] response = null;
            try
            {                
                string url = "http://localhost:5001/";                
                using (var client = new HttpClient())
                {                    
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(url);
                    request.Method = HttpMethod.Get;
                    //request.Content = content;                    
                    response = await client.GetByteArrayAsync(url);                                            
                        Console.WriteLine("количество байт: " + response.Length);                                            
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return response;
        }
        //------------------------------------------------
        private string GetMacAddress()
        {
            string macAddresses = string.Empty;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }

            return macAddresses;
        }
        //------------------------------------------------
    }
}
