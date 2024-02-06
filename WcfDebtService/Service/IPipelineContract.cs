using System.ServiceModel;
using System.ServiceModel.Web;
using Tools.Models;

namespace WcfDebtService
{
    [ServiceContract]
    public interface IPipelineContract
    {
        [OperationContract]
        [WebInvoke]
        string QueueUpPackage(OrderPackage package);
    }
}
