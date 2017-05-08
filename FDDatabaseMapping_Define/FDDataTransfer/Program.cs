using System;
using FDDataTransfer.App.Services;
using FDDataTransfer.Infrastructure.Logger;
using FDDataTransfer.App.Configs;
using System.Text;
using FDDataTransfer.Infrastructure.Runtime;
using System.IO;
using System.Linq;
using FDDataTransfer.Infrastructure.Extensions;

namespace FDDataTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("args length:{0}", args.Length);
            //foreach (var s in args)
            //{
            //    Console.WriteLine(s);
            //}
            //return;
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

                string cmd = string.Empty;
                bool retry = false;
                Menu:
                string[] cmdArgs = args;
                if (cmdArgs.Length == 0 || retry)
                {
                    ShowMenu();
                    cmd = Console.ReadLine();
                    if (cmd.StartsWith("deal", StringComparison.OrdinalIgnoreCase))
                    {
                        cmdArgs = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    retry = false;
                }
                else
                {
                    cmd = cmdArgs[0];
                }

                if (cmd.Equals("deal", StringComparison.OrdinalIgnoreCase) || (cmdArgs.Length > 0 && cmdArgs[0].Equals("deal", StringComparison.OrdinalIgnoreCase)))
                {
                    if (cmdArgs.Length < 2)
                    {
                        Console.WriteLine($"deal业务处理，需传入配置文件（放置于configs目录下，或指定全路径）");
                        goto Menu;
                    }
                    var fileName = cmdArgs[1];
                    var needContinue = !cmdArgs.FirstOrDefault(a => a.Equals("continue", StringComparison.OrdinalIgnoreCase)).IsNullOrEmpty();
                    if (fileName.IndexOf(':') == -1)
                    {
                        fileName = Path.Combine(RuntimeContext.Current.TableConfigPath, fileName);
                    }
                    if (!File.Exists(fileName))
                    {
                        Console.WriteLine($"deal业务处理，传入配置文件{fileName}不存在");
                        goto Menu;
                    }
                    Deal(new DealService(fileName, needContinue));
                }
                else if (cmd.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    Init(() => Deal(new DealService(ConfigManager.Default.TableConfigs[0], false)));
                }
                else if (cmd.IsNullOrEmpty() || cmd.Equals("init", StringComparison.OrdinalIgnoreCase))
                {
                    Init();
                }
                else
                {
                    goto Menu;
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

        static void ShowMenu()
        {
            Console.WriteLine($"输入参数：{Environment.NewLine}  \"init\"：进行数据库数据同步导入（用户基本信息，账户基本信息）；{Environment.NewLine}  \"deal\"：进行数据后续整理（推荐关系，安置关系等）[deal业务处理，需传入配置文件。默认进行全部处理，传入\"continue\"参数进行增量处理]（如：deal xxxconfig.json 或 deal xxxconfig.json continue）；{Environment.NewLine}  \"all\"：先进行数据同步导入操作，当超时完成后；根据同步的第一个配置进行deal操作{Environment.NewLine}回车默认进行数据初始化!{Environment.NewLine}");
            Console.Write("cmd:");
        }

        static void Init(Action finishToDo = null)
        {
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

                    if (writeResult.State == App.Entities.ExecuteState.Success)
                        finishToDo?.Invoke();
                });
                Console.WriteLine($"Server Running config {config.FileName} ...");
            }
        }

        static void Deal(IDealService dealService)
        {
            dealService.Run(result =>
            {
                string msg = $"Deal Running State:{result.State},{result.Message}";
                if (result.Exception != null)
                    msg += $",Exception:{result.Exception.Message},{result.Exception.StackTrace}";
                Console.WriteLine(msg);
            });
        }
    }
}