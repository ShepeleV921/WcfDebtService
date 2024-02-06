using System.Threading.Tasks;
using System;
using OrderPipeline;
using Tools;
using System.Threading;
using System.Data.SqlClient;
using PreparePipeline;
using LoadPipeline;
using Tools.DAL;
using NLog;

namespace WcfDebtService.App_Code
{
    public static class Initializer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Action InvokeService;
        private static IInvoker Invoker;

        static Initializer()
        {
            InvokeService = Repository.SetFreeOnStart;
            InvokeService += InvokePreparingAsync;
            InvokeService += InvokeOrderingAsync;
            InvokeService += InvokeLoaderAsync;
        }

        public static void AppInitialize()
        {
            Logger.Info("Запускаю сервис");

            while (!CanGetConnection())
            {
                Thread.Sleep(60 * 1000);
            }

            InvokeService.Invoke();

        }

        private static async void InvokePreparingAsync()
        {
            Invoker = new PreparingPipeline();
            await Task.Run(() => Invoker.Invoke());
        }

        private static async void InvokeOrderingAsync()
        {
            Invoker = new OrderingPipeline();
            await Task.Run(() => Invoker.Invoke());
        }

        private static async void InvokeLoaderAsync()
        {
            Invoker = new LoaderPipeline();
            await Task.Run(() => Invoker.Invoke());
        }


        private static bool CanGetConnection()
        {
            using (SqlConnection connection = new SqlConnection(SETTINGS.PIPELINE_DB_CONNECTION))
            {
                try
                {
                    connection.Open();
                    Logger.Info("База данных активна");
                    
                    return true;
                }
                catch
                {
                    Logger.Error("Нет подключения к базе данных. Попробую снова через 1 минуту.");
                    
                    return false;
                }
            }
        }
    }
}