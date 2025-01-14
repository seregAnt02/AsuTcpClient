using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ChatClient.Model;

namespace ChatClient.Video
{
    class ProcInfoStart
    {
        internal Directory_info_video Directory_info_video { get; set; }
        public ProcInfoStart(Directory_info_video directory_Info_video)
        {
            Directory_info_video = directory_Info_video;            
        }
        //--------------------------------------------
        //--------------------------------------------        
        internal void Proc_cmd(AdressVideoChannel adressVideo) {
            try
            {
                // обнуляем стартовую временную переменную
                //directory_info_video.Time_start = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 20, 0));

                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.FileName = @"C:\Windows\System32\cmd.exe";
                procInfo.UseShellExecute = false;
                procInfo.RedirectStandardInput = true;
                procInfo.WorkingDirectory = @"c:\Users\name_user\source\repos\ChatClient\ChatClient\bin\Debug\video_" + adressVideo.Index_file + "\\";
                //procInfo.WorkingDirectory = @"c:\Users\name_user\source\repos\ChatClient\ChatClient\bin\Release\video\\";                
                using (Process program = Process.Start(procInfo))
                {
                    //ожидает процесс заданное кол. времяни
                    program.WaitForExit(1000);
                    //запись команды в поток.       
                    CommandInput(adressVideo, program);                                        
                    program.Close();
                    program.Dispose();
                    adressVideo = null;
                    procInfo = null;                    
                }             
            }
            catch (Exception ex)
            {                
                Console.Write(ex.Message + "\r\nзакрытие процесса \"ffmpeg.exe\" ");
                Kill_proc();
            }            
        }
        //--------------------------------------------
        internal void Kill_proc()
        {
            Process[] proc_mas = Process.GetProcesses();
            foreach (Process proc in proc_mas)
                if (proc.ProcessName == "ffmpeg") {
                    proc.Kill();
                    proc.Close();
                    proc.Dispose();
                    Thread.Sleep(250);
                }
            proc_mas = null;
        }
        //--------------------------------------------
        void CommandInput(AdressVideoChannel adressVideo, Process process)
        {            
            string path = Environment.CurrentDirectory;
            string input_adress = @"rtsp://192.168.0.89:554/user=***&password=***&channel=" + adressVideo.Channel + "&stream=" + adressVideo.Stream_tcp;
            string output_adress = path + "\\video_" + adressVideo.Index_file + "\\dash.mpd";
            string commands = @"ffmpeg -rtsp_transport tcp -hide_banner -i " + "\"" + input_adress + "\"" + " -r 25 -c:v copy -ss 00:01 -f dash -y " + "\"" + output_adress + "\"";
            using (StreamWriter pWriter = process.StandardInput)
            {
                if (pWriter.BaseStream.CanWrite) pWriter.WriteLine(commands);

                pWriter.Close();
                pWriter.Dispose();
            }
            adressVideo = null;
            process = null;
            path = null;
            input_adress = null;
            output_adress = null;
            commands = null;
        }
        //--------------------------------------------
        //--------------------------------------------        
    }
}
