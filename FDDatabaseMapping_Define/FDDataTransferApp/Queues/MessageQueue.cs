using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Core.Queues
{
    public class MessageQueue<T> : IMessageQueue<T>
    {
        private Queue<T> _queue;
        public List<T> FailureMessages { get; set; }

        public int Count { get { return _queue.Count; } }

        public MessageQueue()
        {
            _queue = new Queue<T>();
            FailureMessages = new List<T>();
        }

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }
    }
}
