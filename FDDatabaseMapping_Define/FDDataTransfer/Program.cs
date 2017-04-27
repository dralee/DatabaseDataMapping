using System;
using FDDataTransfer.App.Services;
using FDDataTransfer.Infrastructure.Logger;
using FDDataTransfer.App.Configs;
using System.Text;

namespace FDDataTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //var context = new OperConext<Transfer>();
                //    new DBConfig
                //{
                //    ServerConnStringFrom = "server=localhost;database=fdtest;userId=root;password=1234",
                //    ServerConnStringTo = "server=.;initial catalog=testdd;uid=sa;password=123"
                //});
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                ConfigManager.Default.Init();

                foreach (var config in ConfigManager.Default.TableConfigs)
                {
                    //ITransferService transferService = new TransferService("TableMapping.json");
                    ITransferService transferService = new TransferService(config);
                    transferService.Run(readResult =>
                    {
                        string msg = $"Read Running State:{readResult.State},{readResult.Message}";
                        if (readResult.Exception != null)
                            msg += $",Exception:{readResult.Exception.Message},{readResult.Exception.StackTrace}";
                        Console.WriteLine(msg);
                    }, writeResult =>
                    {
                        string msg = $"Write Running State:{writeResult.State},{writeResult.Message}";
                        if (writeResult.Exception != null)
                            msg += $",Exception:{writeResult.Exception.Message},{writeResult.Exception.StackTrace}";
                        Console.WriteLine(msg);
                    });
                    Console.WriteLine($"Server Running config {config.FileName} ...");
                }

                //ServiceBase<Entity> serviceFrom = new ServiceBase<Entity>(new ReadWriteRepository<Entity>(context.FromContext));
                //Entity e = serviceFrom.Get(1);
            }
            catch (Exception ex)
            {
                LoggerManager.Log("Running exception", ex);
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }
    }
}