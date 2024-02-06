using NLog;
using System;
using Tools.DAL;
using Tools.Models;

namespace WcfDebtService
{
    // Не нужный код
    public class PipelineService : IPipelineContract
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string QueueUpPackage(OrderPackage package)
        {
            try
            {
                Logger.Info($"Получено {package.Orders.Count} новых записей");

                if (package.Orders.Count == 0 || package.Orders == null)
                    return "Ошибка. В отправленном запросе не найдено ни одной выписки.";

                if (Repository.ResolveKey(package.Source_Key) == null)
                    return "Ошибка. Неверно указан персональный ключ.";

                bool success = Repository.QueueUpOrders(package);

                if (!success)
                    return "Не удалось принять запрос в обработку.";

                return $"{package.Orders.Count} выписок были приняты в обработку.";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return "Что-то сломалось";
            }
        }
    }
}