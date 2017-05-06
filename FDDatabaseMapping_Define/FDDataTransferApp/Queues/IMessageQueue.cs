using System.Collections.Generic;

namespace FDDataTransfer.App.Core.Queues
{
    public interface IMessageQueue<T>
    {
        List<T> FailureMessages { get; set; }
        int Count { get; }
        void Enqueue(T item);
        T Dequeue();
    }
}
