using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatClient.Video
{
    class ReferenceToVideo
    {
        private Socket socket;
        private HttpObject http_object;
        public ReferenceToVideo(Socket socket, HttpObject http_obj)
        {
            this.socket = socket;
            http_object = http_obj;
        }
        //------------------------------------------------
        //------------------------------------------------
        public void Listner()
        {
            HttpListenAsync();
            //Task rezult = TcpListener();
        }
        //------------------------------------------------        
        private async Task TcpListener()
        {
            TcpListener server = null;
            const int port = 5001; // порт для прослушивания подключений
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);

                // запуск слушателя
                server.Start();

                while (true)
                {
                    Console.WriteLine("Ожидание подключений... ");
                    // получаем входящее подключение
                    using (TcpClient tcp_client = await server.AcceptTcpClientAsync())
                    {
                        Console.WriteLine("Подключен клиент. Выполнение запроса...");

                        // получаем сетевой поток для чтения и записи
                        NetworkStream stream = tcp_client.GetStream();

                        //http_object.TcpRequest("ip", port);
                        
                        SendTcpData(stream);                        

                        // закрываем поток
                        stream.Close();
                        // закрываем подключение
                        tcp_client.Close();                        
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (server != null)
                    server.Stop();
            }
        }
        //------------------------------------------------
        void SendTcpData(NetworkStream stream)
        {
            // сообщение для отправки клиенту
            string response = "Привет мир";
            // преобразуем сообщение в массив байтов
            byte[] data = Encoding.UTF8.GetBytes(response);

            // отправка сообщения
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Отправлено сообщение: {0}", response);
        }
        //------------------------------------------------
        private async void HttpListenAsync()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add("http://localhost:5001/");
                listener.Start();
                Console.WriteLine("Серрвер HTTP запущен. Ожидание подключений...");
                bool listen = true;
                // создаем ответ в виде кода html                
                while (listen)
                {
                    try
                    {
                        // метод GetContext блокирует текущий поток, ожидая получение запроса 
                        HttpListenerContext context = await listener.GetContextAsync();
                        HttpListenerRequest request = context.Request;
                        // получаем объект ответа
                        using (HttpListenerResponse response = context.Response)
                        {
                                                        
                            //ответ
                            //SendToBrowser(response);

                            Console.WriteLine("статус ответа http: " + response.StatusDescription + "\r\n");

                            // останавливаем прослушивание подключений                                                
                            response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        listener.Stop();
                        Console.WriteLine("Обработка подключений завершена \r\n" + ex.Message);
                    }
                }
            }
        }
        //------------------------------------------------
        void SendToBrowser(HttpListenerResponse response)
        {
            // создаем ответ в виде кода html
            string html = "<html><head><meta charset='utf8'></head><body>Привет мир!</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            // получаем поток ответа и пишем в него ответ
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }
        //------------------------------------------------
        //------------------------------------------------
    }
}
