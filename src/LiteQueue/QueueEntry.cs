/* Copyright 2018 by Nomadeon Software LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteQueue
{
    public class QueueEntry<T>
    {
        public long Id { get; set; }
        public T Payload { get; set; }
        public bool IsCheckedOut { get; set; }

        public QueueEntry()
        {

        }

        public QueueEntry(T payload)
        {
            Payload = payload;
        }
    }
}
