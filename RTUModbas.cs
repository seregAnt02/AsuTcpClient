using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Xml.Linq;
using ChatClient.Model;

namespace ChatClient
{
    class RTUModbas
    {
        private protected Socket Socket { get; }
        private PduPackages pduPackages;
        public RTUModbas(Socket socket)
        {
            Socket = socket;
            pduPackages = new PduPackages();
        }
        //====================================================]                
        //====================================================]
        private SerialPort sp;
        internal void PortLoad()
        {
            try
            {
                sp = new SerialPort("COM3", 115200, Parity.None, 8, StopBits.One);
                sp.Open();
            }
            catch (Exception ex)
            {
                sp.Close();
                sp.Dispose();
                sp = null;
                Console.WriteLine(ex.Message);
                //MessageBox.Show(this, ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        //====================================================]
        internal List<string> SendMsg(string pdu)
        {
            try
            {
                ParsingPdu(pdu);
                //if (output == "exit") break;

                // пакет modBus
                byte[] frame = Frame();
                sp.Write(frame, 0, frame.Length);//Send frame  
                      
                Thread.Sleep(1000);// !!!

                byte[] buffRec = new byte[sp.BytesToRead];
                int numberOfBytes = sp.Read(buffRec, 0, buffRec.Length);
                List<string> reqMsg = new List<string>();
                List<string> respMsg = new List<string>();
                //Receiver string
                foreach (var item in frame)
                {
                    reqMsg.Add(string.Format("{0:X2} ", item));
                }
                //Respone string
                foreach (var item in buffRec)
                {
                    respMsg.Add(string.Format("{0:X2} ", item));
                }
                if (reqMsg != null && respMsg != null)
                    if (reqMsg[0] == respMsg[0] && reqMsg[1] == respMsg[1]) {
                        pdu = null;
                        frame = null;
                        buffRec = null;
                        reqMsg = null;

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        return respMsg;
                    }
                pdu = null;
                frame = null;
                buffRec = null;
                reqMsg = null;
                respMsg = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                if (sp != null && sp.IsOpen) {
                    sp.Close();
                    sp.Dispose();
                }
                Console.WriteLine(ex.Message);

                //Console.ReadLine();
            }
            return null;
        }        
        //====================================================]
        private void ParsingPdu(string pdu) {
            
            pduPackages.hing_volume = null;
            pduPackages.low_volume = null;

            string[] mas = pdu.Split(' ');
            pduPackages.slave_adress = byte.Parse(mas[0]); //16 dec
            pduPackages.function_code = Convert.ToInt16(mas[1]);
            pduPackages.start_adress_high = Convert.ToInt16(mas[2], 16);// hex
            pduPackages.start_adress_low = Convert.ToInt16(mas[3], 16);// hex
            pduPackages.high_count = Convert.ToInt16(mas[4], 16);
            pduPackages.low_count = Convert.ToInt16(mas[5], 16);
            pduPackages.hing_volume = pduPackages.hing_volume == null && mas.Length > 6 ? mas[6] : "0x00";
            pduPackages.low_volume = pduPackages.low_volume == null && mas.Length > 6 ? mas[7] : "0x00";

            mas = null;
            pdu = null;
        }

        //====================================================]        
        private byte[] Frame() {
            string[] hiVolume = pduPackages.hing_volume.Split(';');
            string[] loVolume = pduPackages.low_volume.Split(';');
            int y = 4; int countByte = 0;
            byte[] frame = new byte[8];
            //15 (0x0F) Запись нескольких DO
            //16 (0x10) Запись нескольких AO
            if (pduPackages.function_code != 15 && pduPackages.function_code != 16)
            {                
                for (int x = 0; x < loVolume.Length; x++)
                {
                    frame[y++] = (byte)Convert.ToInt16(hiVolume[x], 16);
                    frame[y++] = (byte)Convert.ToInt16(loVolume[x], 16);
                }
            }
            if (pduPackages.function_code == 15)
            {
                countByte = pduPackages.high_count + pduPackages.low_count / 8; if (countByte > 0) countByte++;
                frame = new byte[8 + countByte]; y = 6;
                for (int x = 0; x < loVolume.Length; x++)
                {
                    frame[y++] = (byte)Convert.ToInt16(loVolume[x], 16);
                }
            }
            if (pduPackages.function_code == 16)
            {
                countByte = pduPackages.high_count + pduPackages.low_count * 2;
                frame = new byte[8 + countByte]; y = 6;
                for (int x = 0; x < loVolume.Length; x++)
                {
                    frame[y++] = (byte)Convert.ToInt16(hiVolume[x], 16);
                    frame[y++] = (byte)Convert.ToInt16(loVolume[x], 16);
                }
            }
            frame[0] = pduPackages.slave_adress;
            frame[1] = (byte)pduPackages.function_code;
            frame[2] = (byte)pduPackages.start_adress_high;
            frame[3] = (byte)pduPackages.start_adress_low;            
            frame[4] = (byte)pduPackages.high_count;
            frame[5] = (byte)pduPackages.low_count;            
            byte[] checkSum = CRC16(frame);
            frame[y++] = checkSum[0];
            frame[y] = checkSum[1];

            hiVolume = null;
            loVolume = null;            
            checkSum = null;            

            return frame;
        }
        //====================================================]
        private byte[] CRC16(byte[] data)//конторольная сумма
        {
            byte[] checkSum = new byte[2];
            ushort rec_crc = 0XFFFF;
            for (int i = 0; i < data.Length - 2; i++)
            {
                rec_crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((rec_crc & 0x01) == 1)
                    {
                        rec_crc = (ushort)((rec_crc >> 1) ^ 0xA001);
                    }
                    else
                    {
                        rec_crc = (ushort)(rec_crc >> 1);
                    }
                }
                checkSum[1] = (byte)((rec_crc >> 8) & 0xFF);
                checkSum[0] = (byte)(rec_crc & 0xFF);
            }

            data = null;

            return checkSum;
        }
        //====================================================]
        //состояние значения Low и Hi 
        internal string SignalState(string meaning) {
            string data = null;
            if (meaning == "0") data = "00 00";
            if (meaning == "1") data = "FF 00";
            if (meaning == "FF" || meaning == "01") data = "1";// !! переделать
            if (meaning == "00") data = "0";
            return data;
        }
        //====================================================]
    }
}
