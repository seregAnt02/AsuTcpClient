using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using SendInfoHeader;
using ChatClient.Model;

namespace ChatClient.Video
{
    class Directory_info_video
    {
        private readonly Socket socket;

        internal SendInfo SendInfo { get; set; } = new SendInfo();
        public Directory_info_video(Socket socket)
        {
            this.socket = socket;
            SendInfo.Parameter = new SendInfoHeader.Models.Parameter();
        }                    
        //--------------------------------------------        
        /// <summary>
        /// Метод упрощенного создания заголовка с информацией о размере данных отправляемых по сети.
        /// </summary>
        /// <param name="length">длина данных подготовленных для отправки по сети</param>
        /// <returns>возращает байтовый массив заголовка</returns>
        //--------------------------------------------            
        //--------------------------------------------
        private byte[] GetHeader(int length) {
            string header = length.ToString();
            if (header.Length < 8) {
                string zeros = null;
                for (int i = 0; i < (8 - header.Length); i++) {
                    zeros += "0";
                }
                header = zeros + header;
            }            
            return Encoding.Unicode.GetBytes(header);
        }
        //--------------------------------------------                                        
        internal void SendFiles() {            

            //Time_start = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 20, 0));
            string path = Environment.CurrentDirectory;
            DirectoryInfo dir = new DirectoryInfo(path);

            DirectoryInfo[] directoryInfo = dir.GetDirectories();

            TimeSpan start_closing_app_time = DateTime.Now.TimeOfDay;
            TimeSpan closing_app_time = start_closing_app_time + new TimeSpan(0, 20, 0);

            while (true) {                

                FileInfo[] fileInfos_0 = directoryInfo[1].GetFiles();

                Sending_file(fileInfos_0, new AdressVideoChannel { Index_file = 0, Channel = 1, Stream_tcp = 0 });

                FileInfo[] fileInfos_1 = directoryInfo[2].GetFiles();

                Sending_file(fileInfos_1, new AdressVideoChannel { Index_file = 1, Channel = 2, Stream_tcp = 0 });

                FileInfo[] fileInfos_2 = directoryInfo[3].GetFiles();

                Sending_file(fileInfos_2, new AdressVideoChannel { Index_file = 2, Channel = 3, Stream_tcp = 0 });

                FileInfo[] fileInfos_3 = directoryInfo[4].GetFiles();

                Sending_file(fileInfos_3, new AdressVideoChannel { Index_file = 3, Channel = 4, Stream_tcp = 0 });

                fileInfos_0 = null; fileInfos_1 = null; fileInfos_2 = null; fileInfos_3 = null;                                

                if (DateTime.Now.TimeOfDay >= closing_app_time) {
                    socket.ProcInfoStart.Kill_proc();
                    path = null;
                    dir = null;
                    directoryInfo = null;
                    start_closing_app_time = TimeSpan.Zero;
                    closing_app_time = TimeSpan.Zero;                    
                    break;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Sleep(3000);
            }
        }
        //--------------------------------------------        
        private void Sending_file(FileInfo[] fileInfo, AdressVideoChannel adressVideo) {
            try {
                // Получяем все файлы  и оправка видео-аудио данных            
                for (int x = 0; x < fileInfo.Length; x++) {
                    if (fileInfo[x].Extension == ".m4s" || fileInfo[x].Extension == ".mpd") {
                        //FileInfo copy_file = fileInfo[x].CopyTo($"{fileInfo[x].DirectoryName}\\" + fileInfo[x].Name.Split('.')[0] + ".mp4_tmp", true);                        
                        //fileInfo[x].MoveTo(path_remove_files.FullName + "\\" + fileInfo[x].Name);
                        Thread.Sleep(100);

                        using (FileStream stream = fileInfo[x].Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete | FileShare.None)) {                            

                            //отправка видео файлов на vds                         
                            SendData(fileInfo[x], stream, adressVideo.Index_file);
                            
                            Console.WriteLine("\n******************\n");
                            Console.WriteLine("Имя файла: " + stream.Name);
                            Console.WriteLine("Размер файла: " + stream.Length);                            
                            
                            stream.Close();
                            stream.Dispose();

                            // удаление файла                    
                            Console.Write("Удаление файла: " + fileInfo[x].Name + "\n\r");
                            fileInfo[x].Delete();
                            fileInfo[x] = null;

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }                    
                }
                fileInfo = null;
                adressVideo = null;
            }                        
            catch (Exception ex) {
                Console.Write(ex.Message);
            }
        }
        //--------------------------------------------      
        internal enum Send_info { get, set }
        internal Send_info Send_info_enum { get; set; }
        private void SendData(FileInfo original_file, FileStream stream_file, int index_file) {
            // Состав отсылаемого универсального сообщения
            // 1. Заголовок о следующим объектом класса подробной информации дальнейших байтов
            // 2. Объект класса подробной информации о следующих байтах
            // 3. Байты непосредственно готовых к записи в файл или для чего-то иного.   

            Send_info_enum = Send_info.set;

            FileInfo fi = new FileInfo(stream_file.Name);
            SendInfo.Message = "текст сообщения";            
            SendInfo.Filesize = (int)fi.Length;
            SendInfo.Filename = fi.Name.Split('.')[0];
            SendInfo.Extension = original_file.Extension;
            SendInfo.Index_file = index_file;
            SendInfo.Data = new byte[SendInfo.Filesize];            
            SendInfo.Parameter.id = 13;            

            int count = stream_file.Read(SendInfo.Data, 0, SendInfo.Filesize);
            if (count > 0) {
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream()) {

                    bf.Serialize(ms, SendInfo);                    

                    ms.Position = 0;
                    byte[] infobuffer = new byte[ms.Length];
                    int r = ms.Read(infobuffer, 0, infobuffer.Length);
                    ms.Close();
                    ms.Dispose();
                    if (r > 0) {
                        byte[] header = GetHeader(infobuffer.Length);//Encoding.Unicode.GetBytes(infobuffer.Length.ToString());//GetHeader(infobuffer.Length);
                        byte[] total = new byte[header.Length + infobuffer.Length];

                        Buffer.BlockCopy(header, 0, total, 0, header.Length);
                        Buffer.BlockCopy(infobuffer, 0, total, header.Length, infobuffer.Length);

                        // Так как данный метод вызывается в отдельном потоке рациональней использовать синхронный метод отправки
                        socket.TcpClientData.Net_stream.Write(total, 0, total.Length);

                        // Подтверждение успешной отправки
                        Console.Write("Данные успешно отправлены!\r\n");

                        // Обнулим все ссылки на многобайтные объекты и попробуем очистить память                                                        
                        header = null;
                        total = null;
                        infobuffer = null;
                        bf = null;
                    }                                        
                }
            }            
            fi = null;
            SendInfo.Message = null;
            SendInfo.Filesize = 0;
            SendInfo.Filename = null;
            SendInfo.Extension = null;
            SendInfo.Index_file = 0;
            SendInfo.Data = null;
            original_file = null;
            stream_file = null;
            index_file = 0;

            Send_info_enum = Send_info.get;
        }
        //--------------------------------------------
        //--------------------------------------------
    }
}
