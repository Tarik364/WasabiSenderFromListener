using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;
using WasabiSenderFromListener.ModelsLayer;


namespace WasabiSenderFromListener.WorkerLayer
{
    public class ImageSenderClass
    {
        private readonly string accessKey = "935J8Y9PYV1H6GEQ7RE0";
        private readonly string secretKey = "Btn53MwyGMnDCf3j4tiT1ozWBZAfkCB52AswDIgL";
        private readonly string bucketName = "serkonbucket";
        private readonly RegionEndpoint region = RegionEndpoint.EUCentral1;   ///wasabi Amsterdam bölgesinde çalışması için gerekli bölge bilgisini verdik..

        Logger logger = new Logger();

        public async void ClientHazirlaAsync(string Customer, string date, string idName,string ImageType, FileModel dosyaAdlari)
        {
            try
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = "https://s3.eu-central-1.wasabisys.com",
                    UseHttp = false,
                    SignatureVersion = "4",
                };
                var client = new AmazonS3Client(accessKey, secretKey, config);

                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = Customer + "/" + date + "/" + idName + "/" + ImageType + "/" + dosyaAdlari.FileName,
                    FilePath = dosyaAdlari.FilePath
                };

                await  client.PutObjectAsync(putRequest);
                logger.WriteLog("Dosya işlendi");
            }
            catch (Exception ex)
            {
                logger.WriteLog($"HATA DESC: -- " + ex.Message);
            }
        }
        public async Task CreateFolderAsync(string customerName, string fileDateTime, string modeilid)
        {

            // Ana klasör: Müşteri ismi
            string baseKey = $"{customerName}/";

            var wasabiEndpoint = "https://s3.wasabisys.com";

            var config = new AmazonS3Config
            {
                ServiceURL = "https://s3.eu-central-1.wasabisys.com",
                UseHttp = false,
                SignatureVersion = "4",
            };
            var client = new AmazonS3Client(accessKey, secretKey, config);

            try
            {
                string dateKey = $"{baseKey}{fileDateTime}/";

                if (modeilid == "0")
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = dateKey,
                        ContentBody = string.Empty
                    };
                    var response = await client.PutObjectAsync(request);
                }
                else
                {
                    string Modelkey = $"{dateKey}{modeilid}/";

                    var request = new PutObjectRequest          ////istek oluştur
                    {
                        BucketName = bucketName,
                        Key = Modelkey,                         //// Yeni alt öğe
                        ContentBody = string.Empty              //// İçeriği boş, çünkü sadece yol oluşturuyoruz
                    };

                    var response = await client.PutObjectAsync(request);

                }
                logger.WriteLog($"Klasör başarılı bir şekilde oluşturuldu");
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Klasör oluşturulamadı.: {ex.Message}");
            }
        }
    }
}
