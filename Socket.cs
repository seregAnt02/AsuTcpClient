using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net;
using ChatClient.Model;
using ChatClient.Video;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using SendInfoHeader;
using System.IO;

namespace ChatClient
{
    class Socket
    {
        protected internal RTUModbas RTUModbas { get; private set; }        
        //protected internal Parameter Parameter { get; private set; }
        //protected Directory_info_video directory_info_video;
        protected internal ProcInfoStart ProcInfoStart { get; private set; }

        private Directory_info_video directory_info_video;
        protected AutoResetEvent AutoReset = new AutoResetEvent(false);
        internal TcpClientData TcpClientData { get; set; } = new TcpClientData();
        //====================================================        
        //====================================================        
        //private string userName;
        private const string host = "localhost";
        private const int port = 11000;
        //private TcpClient tcp_client;
        //internal NetworkStream Stream { get; private set; }

        /// <summary>
        /// Возможные режимы работы TCP модуля
        /// </summary>
        public enum Mode { indeterminately, Server, Client };

        /// <summary>
        /// Режим работы TCP модуля
        /// </summary>
        //public Mode modeNetwork;

        //====================================================
        internal void Start() {
            //DateTime date = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));                        
            directory_info_video = new Directory_info_video(this);

            ProcInfoStart = new ProcInfoStart(directory_info_video);

            RTUModbas = new RTUModbas(this);
            //Parameter = new Parameter();                                           

            Task task = new Task(() => {

                //протокол modbus локальный сервер            
                RTUModbas.PortLoad();

                while (true) {
                    //трансляция сообщения подключенному клиенту в сеть modbas, в отдельном потоке   
                    //string meaning = RTUModbas.SignalState("1");

                    List<string> pdu = RTUModbas.SendMsg("16 03 02 06 00 01");
                    //List<string> pdu = RTUModbas.SendMsg("16 06 02 06 " + meaning);

                    if(pdu != null && directory_info_video.Send_info_enum != Directory_info_video.Send_info.set) {

                        directory_info_video.SendInfo.Parameter.datetime = DateTime.Now;
                        //var item = string.Format("{0:X2}", pdu[1]);
                        //if (item == "03")
                            directory_info_video.SendInfo.Parameter.parameter = pdu[3] + pdu[4];
                        //if (pdu[1] == "06") directory_info_video.SendInfo.Parameter.parameter = pdu[4] + pdu[5];
                        Console.Write("\n----------------------\n");                        
                        Console.WriteLine("Ответ: " + directory_info_video.SendInfo.Parameter.parameter + "время: " + DateTime.Now.TimeOfDay + "\r\n");
                    }
                    // RTUModbas.SendMsg("16 06 02 06 " + meaning);               
                    pdu = null;
                    Thread.Sleep(3000);
                }
            });            
            //ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 0, Channel = 1, Stream_tcp = 0});
            //ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 1, Channel = 2, Stream_tcp = 0 });
            //ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 2, Channel = 3, Stream_tcp = 0 });
            //ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 3, Channel = 4, Stream_tcp = 0 });
            //Task task = new Task(delegate () { ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 1, Channel = 2, Stream_tcp = 0 }); });
            //task.Start();            

            //directory_info_video.SendFiles();            

            //каммутация данных с помощью сокетов
            Connect();

            // прослушивание заголовка из 16 и байтов,с последующим обновлением данных.
            //TcpClientData.buffer = new byte[TcpClientData.Lengthheader];
            //TcpClientData.Net_stream.BeginRead(TcpClientData.buffer, 0, TcpClientData.buffer.Length, new AsyncCallback(ReadCallback), TcpClientData);
            

            task.Start();

            //ConnectClient("192.168.0.101");

            //socket.RTUModbas.SendMsg();                                                

            Console.ReadLine();                        
        }

        //====================================================        
        private void Connect() {
            Console.Write("Введите свое имя: ");
            string userName = Console.ReadLine();            
            try {
                TcpClientData.TcpClient = new TcpClient();

                // подключает клиента к указанному порту заданного узла.
                TcpClientData.TcpClient.Connect(host, port);

                TcpClientData.Net_stream = TcpClientData.TcpClient.GetStream(); // получаем поток                    

                string message = userName.Trim();
                byte[] data = Encoding.Unicode.GetBytes(message);
                TcpClientData.Net_stream.Write(data, 0, data.Length);                

                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start(); //старт потока

                //запускаем поток на сохранение новых данных на удаленном сервере                
                //remoteToServer = new Thread(SavingToRemoteServer.ModbasToRemoteServer);
                //remoteToServer.Start();                                                

                Console.WriteLine("Добро пожаловать, {0}", userName);

                userName = null;
                message = null;
                data = null;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }            
        }
        //====================================================
        /// <summary>
        /// Попытка асинхронного подключения клиента к серверу
        /// </summary>
        /// <param name="ipserver">IP адрес сервера</param>        
        //====================================================
        /// <summary>
        /// Звуковое сопровождение ошибок.
        /// </summary>
        private void SoundError() {
            Console.Beep(3000, 30);
            Console.Beep(1000, 30);
        }        
        //====================================================
        private XDocument XmlDum() {
            string hostName = Dns.GetHostName();
            IPHostEntry host = Dns.GetHostEntry(hostName);
            //IPAddress ip = new IPAddress()
            XDocument xdoc = new XDocument();
            // создаем первый элемент
            XElement dum = new XElement("dum");
            // создаем атрибут
            XAttribute NameAttrib = new XAttribute("ip", host.AddressList[1]);
            XElement IdElem = new XElement("id", "1");
            XElement MacAdressElement = new XElement("macadress", GetMACAddress());
            XElement PortElem = new XElement("port", port);
            // добавляем атрибут и элементы в первый элемент
            dum.Add(NameAttrib);
            dum.Add(IdElem);
            dum.Add(MacAdressElement);
            dum.Add(PortElem);
            // создаем корневой элемент
            XElement dums = new XElement("dums");
            // добавляем в корневой элемент
            dums.Add(dum);
            // добавляем корневой элемент в документ
            xdoc.Add(dums);
            //сохраняем документ
            //xdoc.Save("dums.xml");
            return xdoc;
        }
        //====================================================
        private protected string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }
            }
            nics = null;
            return sMacAddress;
        }

        //====================================================
        // получение сообщений от сервера      
        private void ReceiveMessage() {

            while (true) {

                try {

                    TcpClientData.buffer = new byte[513];

                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; string message = null;
                    do {

                        bytes = TcpClientData.Net_stream.Read(TcpClientData.buffer, 0, TcpClientData.buffer.Length);

                        if(bytes < global.LENGTHHEADER) {

                            builder.Append(Encoding.Unicode.GetString(TcpClientData.buffer, 0, bytes));
                            message = builder.ToString();

                            int header = Number_of_drains(message);
                            if(header > 0) {

                                //TcpClientData.buffer = 
                                TcpClientData.length_header = header;
                            }
                        }
                        if(bytes > global.LENGTHHEADER) {

                            TcpClientData.Ms.Write(TcpClientData.buffer, 0, bytes);

                            if (TcpClientData.length_header == TcpClientData.Ms.Length) {

                                BinaryFormatter bf = new BinaryFormatter();
                                TcpClientData.Ms.Position = 0;

                                directory_info_video.SendInfo = (SendInfo)bf.Deserialize(TcpClientData.Ms);

                                TcpClientData.Ms.Close();
                                TcpClientData.Ms.Dispose();
                                TcpClientData.Ms = null;
                                TcpClientData.buffer = null;
                                TcpClientData.size_file = 0;
                                TcpClientData.numberOfBytesRead = 0;
                                bf = null;
                                TcpClientData.Ms = new MemoryStream();
                            }
                        }                                                
                    }
                    while (TcpClientData.Net_stream.DataAvailable);// tru , считывает данные. Условие цикла проверяется после выполнения тела цикла.                    

                    Console.Write("Ответ от сервера: " + message + "\r\n");
                    //старт процесса cmd.exe
                    if (message == "video/") {
                        //закрывает все процессы ffmpeg.exe 
                        ProcInfoStart.Kill_proc();

                        ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 0, Channel = 1, Stream_tcp = 0 });
                        ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 1, Channel = 2, Stream_tcp = 0 });
                        ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 2, Channel = 3, Stream_tcp = 0 });
                        ProcInfoStart.Proc_cmd(new AdressVideoChannel { Index_file = 3, Channel = 4, Stream_tcp = 0 });

                        //подготовка и отправка аудио-видео файла на сервер
                        Task task = new Task(() => directory_info_video.SendFiles());
                        task.Start();
                    }

                    builder = null;                    
                    bytes = 0;
                    message = null;
                    //трансформация полученных данных  в xml    
                    //else if (builder.Length > 80) GetXmlToModel(message); else Console.WriteLine(message);                                        
                }
                catch (Exception ex) {
                    Console.WriteLine("Подключение прервано! " + ex.Message); //соединение было прервано
                    Console.ReadLine();
                    Disconnect();
                }
            }
        }
        //====================================================]        
        /// <summary>
        /// метод приема и расшифровки сетевых данных
        /// </summary>
        internal void ReadCallback(IAsyncResult ar) {

            TcpClientData = (TcpClientData)ar.AsyncState;
            try {
                int r = TcpClientData.Net_stream.EndRead(ar);

                if (r <= global.LENGTHHEADER) {

                    //string data = GetMessage(TcpClientData);                    


                }
                if (r > global.LENGTHHEADER) {
                    TcpClientData.numberOfBytesRead += r;
                    // Получим и десериализуем объект с подробной информацией о содержании получаемого сетевого пакета                    
                    if (TcpClientData.buffer.Length > 0) {

                        TcpClientData.Ms.Write(TcpClientData.buffer, 0, r);

                        if (TcpClientData.size_file == TcpClientData.Ms.Length) {

                            BinaryFormatter bf = new BinaryFormatter();
                            TcpClientData.Ms.Position = 0;

                            directory_info_video.SendInfo = (SendInfo)bf.Deserialize(TcpClientData.Ms);



                            TcpClientData.Ms.Close();
                            TcpClientData.Ms.Dispose();
                            TcpClientData.Ms = null;
                            TcpClientData.buffer = null;
                            TcpClientData.size_file = 0;
                            TcpClientData.numberOfBytesRead = 0;
                            bf = null;
                            ar = null;
                            TcpClientData.Ms = new MemoryStream();

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
                TcpClientData.buffer = new byte[TcpClientData.length_header];
                TcpClientData.Net_stream.BeginRead(TcpClientData.buffer, 0, TcpClientData.buffer.Length, new AsyncCallback(ReadCallback), TcpClientData);
            }
            catch (Exception ex) {
                Console.Write("метод ReadCallback: " + ex.Message);
            }
        }
        //====================================================]                
        // чтение входящего сообщения и преобразование в строку
        private string GetMessage(TcpClientData tcpClientData) {
            string message = null;
            StringBuilder builder = new StringBuilder();
            string text = Encoding.Unicode.GetString(tcpClientData.buffer, 0, tcpClientData.buffer.Length);
            builder.Append(text);
            if(builder.Length > 0) message = builder.ToString();

            text = null;
            builder = null;
            return message;
        }
        //====================================================]      
        // число изи строки
        int Number_of_drains(string str) {
            int result = 0;
            if (int.TryParse(str, out result)) {
                str = null;
                return result;
            }
            return 0;
        }
        //====================================================]      
        // связывает ответ ModBas в Xml документ и отправляем данные на сервер
        internal void DataBind(List<string> responseModbus) {



            //int newVolume = ReversHiLow(responseModbus);            
            //отправка данных
            //if (ComparisonMeaning(newVolume))
            //{

            //XDocument outPutXDocParameter = XmlParameter(responseModbus);
            //byte[] data = Encoding.Unicode.GetBytes(outPutXDocParameter.ToString());
            //TcpClientData.Net_stream.Write(data, 0, data.Length);


            //client.Client.Send(data);
            //Thread.Sleep(50);
            Console.WriteLine("xml данные отправлены на сервер!!");
            //Console.ReadLine();// !!! технологическая
            //}             

            responseModbus = null;
            //outPutXDocParameter = null;
            //data = null;
        }               
        //====================================================                     
        private void Disconnect() {
            if (TcpClientData.Net_stream != null) TcpClientData.Net_stream.Close();//отключение потока
            if (TcpClientData != null) TcpClientData.TcpClient.Close();//отключение клиента                
            Environment.Exit(0); //завершение процесса
        }
    }
}
