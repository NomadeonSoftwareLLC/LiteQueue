/* Copyright 2024 by Nomadeon LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using System;
using System.Collections.Generic;

namespace LiteQueue
{
    public interface IQueue<T>
    {
        bool IsTransactional { get; }

        void Abort(IEnumerable<QueueEntry<T>> items);
        void Abort(QueueEntry<T> item);
        void Clear();
        void Commit(IEnumerable<QueueEntry<T>> items);
        void Commit(QueueEntry<T> item);
        int Count();
        List<QueueEntry<T>> CurrentCheckouts();
        QueueEntry<T> Dequeue();
        List<QueueEntry<T>> Dequeue(int batchSize);
        void Enqueue(IEnumerable<T> items);
        void Enqueue(T item);
        void ResetOrphans();
        void SetOrder<TKey>(Func<QueueEntry<T>, TKey> selector) where TKey : IComparable;

    }
}