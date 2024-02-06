using System.Threading;

namespace Tools.Models
{
    public class LoadOrder : OrderModel
    {
        public string NumRequest { get; set; }

        public string WorkerKey { get; set; }


        public override string ToString() => $"[ИД потока = {Thread.CurrentThread.ManagedThreadId} ID = {ID},\t Source = {Source},\t NumRequest = {NumRequest},\t Key = {WorkerKey}]";
    }
}