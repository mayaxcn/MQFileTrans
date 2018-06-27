using IBM.WMQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace MQFileTrans
{
    class MQClassWoker
    {
        MQMessage queueMessage;                         //消息
        MQQueue queue;                                  //队列
        MQGetMessageOptions queueGetMessageOptions;
        MQPutMessageOptions queuePutMessageOptions;

        public string HostName = ConfigurationManager.AppSettings["HostName"];
        public int HostPort = Convert.ToInt32(ConfigurationManager.AppSettings["HostPort"]);
        public string SvrCHL = ConfigurationManager.AppSettings["SvrCHL"];
        public string CCSID = ConfigurationManager.AppSettings["CCSID"] == "" ? "1381" : ConfigurationManager.AppSettings["CCSID"];
        public string MQName = ConfigurationManager.AppSettings["MQName"];

        public string RecvQueue = ConfigurationManager.AppSettings["RecvQueue"];
        static string RecvFolder = ConfigurationManager.AppSettings["RecvFolder"];
        public string SendQueue = ConfigurationManager.AppSettings["SendQueue"];
        public string SendFolder = ConfigurationManager.AppSettings["SendFolder"];

        private string saveRecvFile(MQMessage queueMessage, string path)
        {
            string filename = queueMessage.ReadString(200).Trim();
            string fullName = Path.Combine(path, filename);
            FileStream fsNF = new FileStream(fullName, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fsNF);
            bw.Write(queueMessage.ReadBytes(queueMessage.MessageLength - 200));
            bw.Close();
            return filename;
        }

        private byte[] readFileByte(string FullFilename)
        {
            FileStream fsOF = new FileStream(FullFilename, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fsOF);
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] temp = br.ReadBytes((int)br.BaseStream.Length);
            br.Close();
            fsOF.Close();
            return temp;
        }

        private string handleFileName(string filename)
        {
            //参数filename为完整路径
            string tmpfilename = "";
            for (int len = filename.Length; len < 200; len++)
            {
                tmpfilename += " ";
            }
            return filename + tmpfilename;
        }

        private void moveFile(string SrcFileName, string DstFilePath, string FileName)
        {
            string sortDirByDate = DateTime.Now.ToString("yyyyMMdd");
            string bakPath = DstFilePath + "/" + sortDirByDate + "/";   //处理成功后移动报文到当日文件夹下的路径
            if (!Directory.Exists(bakPath))
                Directory.CreateDirectory(bakPath);
            string DstFileName = Path.Combine(bakPath, FileName);
            if (File.Exists(DstFileName))
                File.Delete(DstFileName);
            File.Move(SrcFileName, DstFileName);
            File.Delete(SrcFileName);
        }

        private IList scanDirectory(string path)
        {
            IList list = new ArrayList();
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo file in files)
            {
                list.Add(file.Name);
            }
            return list;
        }


        public void getFileFromMQ()
        {
            if (!String.IsNullOrEmpty(RecvQueue) || !String.IsNullOrEmpty(RecvFolder))
            {
                try
                {
                    IList FileList = new ArrayList();
                    string fileName = "";

                    //初始化环境变量
                    MQEnvironment.Hostname = HostName; //服务器的IP
                    MQEnvironment.Channel = SvrCHL; //服务器连接通道
                    MQEnvironment.Port = HostPort;
                    Environment.SetEnvironmentVariable("MQCCSID", CCSID);
                    string qManager = MQName; //队列管理器
                    string QLName = RecvQueue;  //接收队列

                    MQQueueManager queueManager = new MQQueueManager(qManager);
                    int OutOpenOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_INQUIRE;
                    queueGetMessageOptions = new MQGetMessageOptions();
                    queueGetMessageOptions.Options = MQC.MQGMO_FAIL_IF_QUIESCING | MQC.MQGMO_WAIT;
                    queueGetMessageOptions.MatchOptions = MQC.MQMO_MATCH_CORREL_ID;
                    queueGetMessageOptions.WaitInterval = 500;
                    queueMessage = new MQMessage();
                    queueMessage.Format = MQC.MQFMT_STRING;

                    queue = queueManager.AccessQueue(QLName, OutOpenOptions);
                    while (queue.CurrentDepth > 0)
                    {
                        queue.Get(queueMessage, queueGetMessageOptions);
                        fileName = saveRecvFile(queueMessage, RecvFolder);
                        //if (!FileList.Contains(fileName))
                        //{
                        //    FileList.Add(fileName);
                        //}
                        Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]提取成功:" + fileName + "\r\n");
                    }
                }
                catch (MQException MQExp)
                {
                    Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]提取失败:" + MQExp.Message + "\r\n");
                }
                finally
                {
                    if (queue != null)
                        queue.Close();
                }
            }
            else
            {
                Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]提取失败:无RecvQueue配置\r\n");
            }
        }
        
        public void putFileToMQ()
        {
            if (!String.IsNullOrEmpty(SendQueue) || !String.IsNullOrEmpty(SendFolder))
            {
                try
                {
                    string _MessagePath = SendFolder;
                    string _MessagePathBak = SendFolder + "\\Bak\\";

                    IList sendList = scanDirectory(_MessagePath);
                    string fileName = null;
                    string filePath = null;
                    //初始化环境变量
                    MQEnvironment.Hostname = HostName; //服务器的IP
                    MQEnvironment.Channel = SvrCHL; //服务器连接通道
                    MQEnvironment.Port = HostPort;
                    Environment.SetEnvironmentVariable("MQCCSID", CCSID);
                    string qManager = MQName; //队列管理器
                    string QLName = SendQueue;  //接收队列

                    //***************注意需要在MQ的MCA添加MUSR_MQADMIN***************//
                    //构造队列管理器
                    //int openOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_OUTPUT;
                    int openOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    foreach (String _fileName in sendList)
                    {
                        if (sendList.Count <= 0)
                        {
                            //无新报文跳出循环
                            break;
                        }
                        MQQueueManager queueManager = new MQQueueManager(qManager);
                        queue = queueManager.AccessQueue(QLName, openOptions);
                        queueMessage = new MQMessage();
                        queuePutMessageOptions = new MQPutMessageOptions();

                        fileName = _fileName;
                        filePath = Path.Combine(_MessagePath, fileName);

                        //放入消息
                        queueMessage.ClearMessage();
                        queueMessage.Format = MQC.MQFMT_STRING;
                        queueMessage.WriteString(handleFileName(fileName));
                        queueMessage.Write(readFileByte(filePath));
                        queue.Put(queueMessage, queuePutMessageOptions);
                        //发送成功后移动文件
                        moveFile(filePath, _MessagePathBak, fileName);
                        Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]放入成功:" + fileName + "\r\n");
                    }
                }
                catch (MQException MQExp)
                {
                    Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]放入失败:" + MQExp.Message + "\r\n");
                }
                finally
                {
                    if (queue != null)
                        queue.Close();
                }
            }
            else
            {
                Console.Write("[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "]放入失败:无SendQueue配置\r\n");
            }
        }
    }
}
