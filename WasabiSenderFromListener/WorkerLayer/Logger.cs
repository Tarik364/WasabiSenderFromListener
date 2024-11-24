using System;
using System.IO;

namespace WasabiSenderFromListener.WorkerLayer
{
    public class Logger
    {
        private readonly string logFilePath = @"C:\Serkon\WasabiServiceTCPLog.txt";
        public void WriteLog(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch
            {
            }
        }
    }
}
