using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQFileTrans
{
    //IBMMQ 6.0下测试通过，请使用服务器通道 
    class Program
    {
        public static System.Timers.Timer sendTimer;
        public static System.Timers.Timer recvTimer;
        static void Main(string[] args)
        {
            Console.Write("==========MQ文件传输程序(输入“exit”退出)==========\r\n");

            /////////////////////////发送部分（开始）////////////////////////////////
            Console.Write(DateTime.Now.ToString("[MM-dd HH:mm:ss]") + "发送通道已启动\r\n");
            sendTimer = new System.Timers.Timer();
            sendTimer.Interval = Convert.ToDouble(2500);    //可更改检测目录时间
            sendTimer.Elapsed += new System.Timers.ElapsedEventHandler(sendTimer_Elapsed);
            sendTimer.Enabled = true;
            sendTimer.AutoReset = true;
            /////////////////////////发送部分（结束）////////////////////////////////

            /////////////////////////接收部分（开始）////////////////////////////////
            Console.Write(DateTime.Now.ToString("[MM-dd HH:mm:ss]") + "接收通道已启动\r\n");
            recvTimer = new System.Timers.Timer();
            recvTimer.Interval = Convert.ToDouble(2000);    //可更改检测目录时间
            recvTimer.Elapsed += new System.Timers.ElapsedEventHandler(recvTimer_Elapsed);
            recvTimer.Enabled = true;
            recvTimer.AutoReset = true;
            /////////////////////////接收部分（结束）////////////////////////////////

            string exitCommand = "";
            while (exitCommand != "exit")
            {
                exitCommand = Console.ReadLine();
                if (exitCommand == "exit")
                {
                    Environment.Exit(0);  
                }
            }
        }

        static void sendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                MQClassWoker mqWork = new MQClassWoker();
                mqWork.putFileToMQ();
            }
            catch { }
        }

        static void recvTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                MQClassWoker mqWork = new MQClassWoker();
                mqWork.getFileFromMQ();
            }
            catch { }
        }
    }
}
