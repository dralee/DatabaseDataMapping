using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.IO;

namespace FDDataTransfer.Infrastructure.Logger
{
    public class LoggerManager
    {
        private static ILog _log;
        static LoggerManager()
        {
            ILoggerRepository repository = LogManager.CreateRepository("FDDataTransfer");
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            _log = LogManager.GetLogger(repository.Name, "FDDataTransferLog");
        }

        public static void Log(object message, Exception ex = null)
        {
            
            //ILog log = LogManager.GetLogger(typeof(T));
            if (ex != null)
            {
                _log.Info(message, ex);
            }
            else
            {
                _log.Info(message);
            }
        }
    }
}
