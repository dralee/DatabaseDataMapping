using System;
using FDDataTransfer.App.Services;
using FDDataTransfer.Infrastructure.Logger;
using FDDataTransfer.App.Configs;
using System.Text;
using FDDataTransfer.Infrastructure.Runtime;
using System.IO;
using System.Linq;
using FDDataTransfer.Infrastructure.Extensions;
using FDDataTransfer.App.Entities;

namespace FDDataTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
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
                    if (cmd.StartsWith("deal", StringComparison.OrdinalIgnoreCase)
                        || cmd.StartsWith("recommend", StringComparison.OrdinalIgnoreCase)
                        || cmd.StartsWith("relation", StringComparison.OrdinalIgnoreCase)
                        || cmd.StartsWith("center", StringComparison.OrdinalIgnoreCase)
                        || cmd.StartsWith("usercenter", StringComparison.OrdinalIgnoreCase))
                    {
                        cmdArgs = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    retry = false;
                }
                else
                {
                    cmd = cmdArgs[0];
                }

                Func<bool> needContinue = () => !cmdArgs.FirstOrDefault(a => a.Equals("continue", StringComparison.OrdinalIgnoreCase)).IsNullOrEmpty();
                Func<string, bool> isCmd = name => cmd.Equals(name, StringComparison.OrdinalIgnoreCase) || (cmdArgs.Length > 0 && cmdArgs[0].Equals(name, StringComparison.OrdinalIgnoreCase));
                // 业绩服务处理
                Action<AchievementService> actionFinish = acs =>
                {
                    acs?.Run(
                    readResult =>
                    {
                        ReadResult(readResult);
                    },
                    writeResult =>
                    {
                        WriteResult(writeResult);
                    });
                };

                if (isCmd("deal") || isCmd("recommend") || isCmd("relation") || isCmd("center") || isCmd("usercenter"))
                {
                    if (cmdArgs.Length < 2)
                    {
                        Console.WriteLine($"deal业务处理，需传入配置文件（放置于configs目录下，或指定全路径）");
                        goto Menu;
                    }
                    var fileName = cmdArgs[1];
                    if (fileName.IndexOf(':') == -1)
                    {
                        fileName = Path.Combine(RuntimeContext.Current.TableConfigPath, fileName);
                    }
                    if (!File.Exists(fileName))
                    {
                        Console.WriteLine($"deal业务处理，传入配置文件{fileName}不存在");
                        goto Menu;
                    }
                    bool isContinue = needContinue();
                    DealType type = DealType.All;

                    if (isCmd("recommend"))
                    {
                        type = DealType.Recommend;
                        actionFinish = null;
                    }
                    else if (isCmd("relation"))
                        type = DealType.Relation;
                    else if (isCmd("center"))
                        type = DealType.Center;
                    else if (isCmd("usercenter"))
                        type = DealType.UserCenter;
                    else { }

                    if (type == DealType.Center)
                    {
                        new CenterAchievementService(fileName, isContinue).Run(
                            readResult =>
                            {
                                ReadResult(readResult);
                            },
                            writeResult =>
                            {
                                WriteResult(writeResult);
                            });
                    }
                    else
                    {
                        Deal(new DealService(fileName, isContinue), type, () => actionFinish(new AchievementService(fileName, isContinue)));
                    }
                }
                else if (cmd.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var config = ConfigManager.Default.TableConfigs[0];
                    Init(() => Deal(new DealService(config, false),
                        DealType.All, () => actionFinish(new AchievementService(config, false))));
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
            Console.WriteLine($"输入参数：{Environment.NewLine}  \"init\"：进行数据库数据同步导入（用户基本信息，账户基本信息，中心业绩）；{Environment.NewLine}  \"deal\"：进行数据后续整理（推荐关系，安置关系等）[deal业务处理，需传入配置文件。默认进行全部处理，传入\"continue\"参数进行增量处理]（如：deal xxxconfig.json 或 deal xxxconfig.json continue）；{Environment.NewLine}  \"recommend\"：进行推荐关系数据处理 [recommend业务处理，需传入配置文件。默认进行全部处理，传入\"continue\"参数进行增量处理]（如：recommend xxxconfig.json 或 recommend xxxconfig.json continue）；{Environment.NewLine}  \"relation\"：进行安置关系及业绩数据处理 [relation业务处理，需传入配置文件。默认进行全部处理，传入\"continue\"参数进行增量处理]（如：relation xxxconfig.json 或 relation xxxconfig.json continue）；{Environment.NewLine}  \"center\"：进行服务中心业绩数据处理 [center业务处理，需传入配置文件。如果执行过init操作，则无须此操作]（如：center xxxconfig.json）；{Environment.NewLine}  \"usercenter\"：进行用户中心关系处理 [usercenter业务处理，需传入配置文件。]（如：usercenter xxxconfig.json）；{Environment.NewLine}  \"all\"：先进行数据同步导入操作，当超时完成后；根据同步的第一个配置进行deal操作{Environment.NewLine}回车默认进行数据初始化!{Environment.NewLine}");
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
                    ReadResult(readResult);
                }, writeResult =>
                {
                    WriteResult(writeResult);

                    if (writeResult.State == App.Entities.ExecuteState.Success)
                        finishToDo?.Invoke();
                });
                Console.WriteLine($"Server Running config {config.FileName} ...");
            }
        }

        static void ReadResult(ExecuteResult readResult)
        {
            string msg = $"Read Running State:{readResult.State},{readResult.Message}";
            if (readResult.Exception != null)
                msg += $",Exception:{readResult.Exception.Message},{readResult.Exception.StackTrace}";
            Console.WriteLine(msg);
        }

        static void WriteResult(ExecuteResult writeResult)
        {
            string msg = $"Write Running State:{writeResult.State},{writeResult.Message}";
            if (writeResult.Exception != null)
                msg += $",Exception:{writeResult.Exception.Message},{writeResult.Exception.StackTrace}";
            Console.WriteLine(msg);
        }

        static void Deal(IDealService dealService, DealType type, Action finished = null)
        {
            switch (type)
            {
                case DealType.All:
                    dealService.Run(result =>
                    {
                        string msg = $"Deal Running State:{result.State},{result.Message}";
                        if (result.Exception != null)
                            msg += $",Exception:{result.Exception.Message},{result.Exception.StackTrace}";
                        Console.WriteLine(msg);
                    });
                    break;
                case DealType.Recommend:
                    dealService.RunRecommend(result =>
                    {
                        string msg = $"Recommend Running State:{result.State},{result.Message}";
                        if (result.Exception != null)
                            msg += $",Exception:{result.Exception.Message},{result.Exception.StackTrace}";
                        Console.WriteLine(msg);
                    });
                    break;
                case DealType.Relation:
                    dealService.RunRelation(result =>
                    {
                        string msg = $"Relation Running State:{result.State},{result.Message}";
                        if (result.Exception != null)
                            msg += $",Exception:{result.Exception.Message},{result.Exception.StackTrace}";
                        Console.WriteLine(msg);

                        if (result.ServiceFinished)
                        {
                            finished.Invoke();
                        }
                    });
                    break;
                case DealType.UserCenter:
                    dealService.RunUserCenter(result =>
                    {
                        string msg = $"UserCenter Running State:{result.State},{result.Message}";
                        if (result.Exception != null)
                            msg += $",Exception:{result.Exception.Message},{result.Exception.StackTrace}";
                        Console.WriteLine(msg);
                    });
                    break;
            }

        }
    }

    enum DealType
    {
        All, Recommend, Relation, Center, UserCenter
    }
}