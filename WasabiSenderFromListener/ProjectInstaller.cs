using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace WasabiSenderFromListener
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            AfterInstall += new InstallEventHandler(AfterInstallHandler);
        }
        private void AfterInstallHandler(object sender, InstallEventArgs e)
        {
            ServiceController service = new ServiceController("WasabiSenderListener");
            try
            {
                // Servisi başlat
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Servis başlatılamadı: {ex.Message}");
            }
        }
    }
}
