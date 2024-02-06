using System;
using static System.Threading.Thread;
using NLog;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;

namespace OrderPipeline
{
	public class OrderingPipeline : Pipeline<PreparedOrder>, IInvoker
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected override bool OutOfOrders => Repository.CheckPreparedQueue();

		public void Invoke()
		{
			CurrentThread.Name = "Orderer";

			while (true)
			{
				if (OrderWorker.OutOfCapacity)
				{
					Logger.Info("Все потоки заняты. Отдыхаю 30 с.");
					Sleep(30_000);
					continue;
				}

				if (OutOfOrders)
				{
					Logger.Info("В базе данных отсутствуют подходящие записи для заказа. Отдыхаю 30 с.");
					Sleep(30_000);
					continue;
				}

				try
				{
                    Order = Repository.GetPrepared();
                    Worker = Repository.GetPreparedPipeline();
					Logger.Info($"Взял [{Order.CadastralNumber}] ID = [{Order.ID}] от [{Order.Source}] [{Worker.LoginKey}]");

					IWorker<PreparedOrder> worker = new OrderWorker(Worker, Order);
					worker.Run();
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "отдыхаю 20 с.");
					Sleep(20_000);
				}
			}
		}
	}
}