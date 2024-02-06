using System;
using static System.Threading.Thread;
using NLog;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;


namespace PreparePipeline
{
	public class PreparingPipeline : Pipeline<UnpreparedOrder>, IInvoker
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected override bool OutOfOrders => Repository.CheckUnpreparedQueue();	// Проверяем есть ли в БД записи для заказа


		public void Invoke()
		{
			CurrentThread.Name = "Preparer";

			while (true)
			{
				if (PrepareWorker.OutOfCapacity)	// Если асинхронных обработчиков уже больше определённого кол-ва - отдыхаем
				{
					Logger.Info("Все потоки заняты. Отдыхаю 10 с.");
					Sleep(10_000);
					continue;
				}

				if (OutOfOrders)	// Если в бд нет записей для заказа - отдыхаем
				{
					Logger.Info("В базе данных отсутствуют подходящие записи для подготовки. Отдыхаю 30 с.");
					Sleep(30_000);
					continue;
				}

				try
				{
					Worker = Repository.GetUnpreparedPipeline();	 // Берём ключик от Росреестра
					Order = Repository.GetUnprepared();             // Берём слудующий ордер для заказа
					Logger.Info($"Взял [{Order.Address}] ID = [{Order.ID}] от [{Order.Source}] [{Worker.LoginKey}]");

					IWorker<UnpreparedOrder> worker = new PrepareWorker(Worker, Order);
					worker.Run();   // Запускаем асинхронный обработчик
					Sleep(10_000); // Задержка обработчика 06.02.2023
                }
				catch (Exception ex)
				{
					Logger.Error(ex, "отдыхаю 30 с.");
					Sleep(30_000);
				}
			}
		}
	}
}