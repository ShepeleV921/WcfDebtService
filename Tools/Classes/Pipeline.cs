using Tools.Models;
using Tools.Rosreestr;

namespace Tools.Classes
{
    public abstract class Pipeline<TModel> where TModel : OrderModel
    {
        protected TModel Order { get; set; }

        protected RosreestrPipeline Worker { get; set; }

        protected abstract bool OutOfOrders { get; }
    }
}
