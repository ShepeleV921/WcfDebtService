using Tools.Models;

namespace Tools
{
    public interface IWorker<T> where T : OrderModel
	{
        void Run();
    }
}