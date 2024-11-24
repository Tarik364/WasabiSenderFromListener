using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.ServiceProcess;
using System.Threading.Tasks;
using WasabiSenderFromListener.ModelsLayer;
using WasabiSenderFromListener.WorkerLayer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WasabiSenderFromListener
{
    public partial class WasabiSenderFromListener : ServiceBase
    {
        ImageSenderClass imageSenderClass = new ImageSenderClass();
        Logger logger = new Logger();

        string FilePathFile = @"";
        string FilePathFront = @"";
        string FilePathBack = @"";


        string oldmodelid = "1";
        string newmodelid = "1";

        IPAddress ipaddres = null;
        int port = 0;

        TcpListener dinleyicisoket;

        DateTime olddatetime = DateTime.Now;

        public string Customer = "";
        
        bool IsActive = false;

        private FileSystemWatcher watcher;
        public WasabiSenderFromListener()
        {
            InitializeComponent();
        }
        public void JsonRead()
        {
            try
            {
                // JSON dosyasını oku
                var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);

                // JSON'u parse et
                var jObject = JObject.Parse(json);

                // ConnectionString'i oku
                FilePathFile = jObject["RawImagePath"]["FilePathFile"].ToString();
                oldmodelid = jObject["LastID"]["ID"].ToString();
                Customer = jObject["CustomerName"]["Customer"].ToString();
                ipaddres = IPAddress.Parse(jObject["IpAddress"]["IP"].ToString());
                port = int.Parse(jObject["PORT"]["Port"].ToString());
                logger.WriteLog(ipaddres.ToString() + "---" + port+ "-----"+ FilePathFile);
            }
            catch (Exception ex)
            {
                logger.WriteLog(FilePathFile + "--------" + oldmodelid + "--------" + Customer);
            }
        }
        public void JsonWrite(string ModelID)   /// Kaydedilen ModelID gonder
        {
            try
            {
                // JSON dosyasını oku
                var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);
                var jObject = JObject.Parse(json);

                // Değeri güncelle
                if (jObject["LastID"] != null)
                {
                    jObject["LastID"]["ID"] = ModelID;

                    // JSON'u tekrar dosyaya yaz
                    File.WriteAllText(appSettingsPath, jObject.ToString(Formatting.Indented));
                }
                else
                {
                    logger.WriteLog("Section LastID bulunamadı." + ModelID);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog("Json Write ---- " + ex.Message);
            }

        }
        protected override void OnStart(string[] args)
        {
            try
            {
                JsonRead();
 
                logger.WriteLog("FileSystemWatcher initialized.  ---");
                StartListening();

            }
            catch (Exception ex)
            {
                logger.WriteLog($"Error in OnElapsedTime: {ex.Message}");
            }
        }
        public void StartListening()
        {
            StartServer(); 
            TCP();
        }
        public void StartServer()
        {
            dinleyicisoket = new TcpListener(ipaddres, port);

            dinleyicisoket.Start(); // Dinlemeyi başlat
        }
        public void TCP()
        {
            while (true)
            {
                try
                {
                    if (dinleyicisoket.Pending())
                    {
                        Socket istemciSoketi = dinleyicisoket.AcceptSocket();

                        using (NetworkStream agAkisi = new NetworkStream(istemciSoketi))
                        {
                            using (BinaryReader binaryOkuyucu = new BinaryReader(agAkisi))
                            {
                                string dModelID = binaryOkuyucu.ReadString();

                                if (!string.IsNullOrEmpty(dModelID))
                                {
                                    Task.Run(() => SocketListener(dModelID));
                                }
                            }
                        }
                        istemciSoketi.Close();
                        istemciSoketi.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    // BURAYA HATA YAZDIRMA YAPARSAN SOKETTTEN GELEN ANLIK HATALAR LOG TABLOSUNU VE ARAYUZU KITLIYOR
                    // PLC DE KITLENIYOR - O SEBEPLE BURASI BOŞ KALMALI !
                }
            }
        }
        public async void SocketListener(string modelid)
        {
            if (IsActive == true)
            {
                return;
            }
            IsActive = true;

            newmodelid = modelid;
            if (newmodelid != oldmodelid)       /////YENİ İSE ÇALIŞ
            {
                if (olddatetime.ToString("dd-MM-yyyy") != DateTime.Now.ToString("dd-MM-yyyy"))
                {
                    olddatetime = DateTime.Now;             //////Gün değişikliği yaşandı servis kapanmamıştı ve bugüne eşitleyip yeni klasör oluşturuyoruz wasabide.
                    await imageSenderClass.CreateFolderAsync(Customer, olddatetime.ToString("dd-MM-yyyy"), "0");
                }
                await imageSenderClass.CreateFolderAsync(Customer, olddatetime.ToString("dd-MM-yyyy"), newmodelid);
                oldmodelid = newmodelid;
                JsonWrite(newmodelid);

                FilePathFront = FilePathFile + $"\\{newmodelid}\\test\\front";
                FilePathBack = FilePathFile + $"\\{newmodelid}\\test\\back";

                logger.WriteLog("Front yolu ---" + FilePathFront + "-----" + "Back yolu ---" + FilePathBack);


                _ = Task.Run(() =>
                {
                    ProcessFile(FilePathFront, "Front");     ////FRONT KLASÖRÜNÜ BAS
                    ProcessFile(FilePathBack, "Back");       ////BACK KLASÖRÜNÜ BAS
                });
            }
            else
            {
                FilePathFront = FilePathFile + $"\\{newmodelid}\\test\\front";
                FilePathBack = FilePathFile + $"\\{newmodelid}\\test\\back";

                logger.WriteLog("Front yolu ---" + FilePathFront + "-----" + "Back yolu ---" + FilePathBack);
                _ = Task.Run(() =>
                {
                    ProcessFile(FilePathFront, "Front");     ////FRONT KLASÖRÜNÜ BAS
                    ProcessFile(FilePathBack, "Back");       ////BACK KLASÖRÜNÜ BAS
                });
            }
            IsActive = false;
        }
        private async void ProcessFile(string filePath, string ImageType)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    string[] files = Directory.GetFiles(filePath);
                    foreach (string file in files)
                    {
                        FileModel fileModel = new FileModel
                        {
                            FilePath = file,                            // Dosyanın tam yolu
                            FileName = Path.GetFileName(file)           // Sadece dosya adı
                        };
                        logger.WriteLog("Yeni dosya işleniyor: " + fileModel.FileName);
                        imageSenderClass.ClientHazirlaAsync(Customer, olddatetime.ToString("dd-MM-yyyy"), newmodelid, ImageType, fileModel);      ////////wasabiye gönder

                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLog("Hata oluştu: " + ex.ToString());
                }
            });
        }
        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
        }
        protected override void OnStop()
        {
        }
    }
}
