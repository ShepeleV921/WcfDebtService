using System;
using static System.Threading.Thread;
using NLog;
using Tools;
using Tools.Classes;
using Tools.DAL;
using Tools.Models;
using Tools.Rosreestr;

namespace LoadPipeline
{
	public class LoaderPipeline : Pipeline<LoadOrder>, IInvoker
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected override bool OutOfOrders => Repository.CheckLoadQueue();


		public void Invoke()
		{
			CurrentThread.Name = "Loader";

			while (true)
			{
				if (LoadWorker.OutOfCapacity)
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
					Order = Repository.GetLoadable();
					Worker = new RosreestrPipeline(Order.WorkerKey);
					Logger.Info($"Взял [{Order.NumRequest}] ID = [{Order.ID}] от [{Order.Source}] [{Order.WorkerKey}]");

					IWorker<LoadOrder> worker = new LoadWorker(Worker, Order);
					worker.Run();
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