/* Copyright 2024 by Nomadeon LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using LiteDB;
using LiteQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteQueueTests
{
    /// <summary>
    /// Many tests may appear similar to the Transactional tests but there are subtle differences
    /// </summary>
    [TestClass]
    public class LiteQueueTest_NotTransactional
    {
        LiteDatabase _db;
        const string _collectionName = "nottransactionaltestcollection";

        LiteQueue<T> CreateQueue<T>()
        {
            var logCollection = _db.GetCollection<QueueEntry<T>>(_collectionName);
            var logs = new LiteQueue<T>(logCollection, false);
            return logs;
        }

        [TestInitialize]
        public void Init()
        {
            _db = new LiteDatabase("Filename=LiteQueueTest.db;connection=shared");
            _db.DropCollection(_collectionName);
        }

        [TestMethod]
        public void Ctor_DbCollectionName()
        {
            var logs = new LiteQueue<string>(_db, _collectionName);

            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        public void Ctor_Collection()
        {
            var logs = CreateQueue<string>();

            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        public void Dequeue()
        {
            var logs = CreateQueue<string>();

            const string entry = "NextTest";
            logs.Enqueue(entry);

            var record = logs.Dequeue();
            Assert.IsFalse(record.IsCheckedOut);
            Assert.AreEqual(entry, record.Payload);
            Assert.AreEqual(0, logs.Count());

            record = logs.Dequeue();
            Assert.IsNull(record);
        }

        [TestMethod]
        public void DequeueBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("a", records[0].Payload);
            Assert.AreEqual(2, logs.Count());

            records = logs.Dequeue(2);
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("b", records[0].Payload);
            Assert.AreEqual("c", records[1].Payload);
            Assert.AreEqual(0, logs.Count());

            records = logs.Dequeue(2);
            Assert.AreEqual(0, records.Count);
        }

        [TestMethod]
        public void Fifo()
        {
            var logs = CreateQueue<int>();

            const int count = 1000;

            for (int i = 0; i < count; i++)
            {
                logs.Enqueue(i);
            }

            for (int i = 0; i < count; i++)
            {
                int next = logs.Dequeue().Payload;
                Assert.AreEqual(i, next);
            }

            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CurrentCheckouts()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);
            var checkouts = logs.CurrentCheckouts();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ResetOrphans()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);

            logs.ResetOrphans();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Abort()
        {
            var logs = CreateQueue<string>();

            logs.Enqueue("AddTest");

            var record = logs.Dequeue();
            logs.Abort(record);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AbortBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(3);
            logs.Abort(records);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Commit()
        {
            var logs = CreateQueue<string>();

            logs.Enqueue("AddTest");

            var record = logs.Dequeue();
            logs.Commit(record);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CommitBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(3);
            logs.Commit(records);
        }

        [TestMethod]
        public void Clear()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);

            logs.Clear();
            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        public void ComplexObject()
        {
            var logs = CreateQueue<CustomRecord>();

            var batch = SampleData.GetCustomRecords();

            logs.Enqueue(batch);

            var records = logs.Dequeue(1);
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(2, logs.Count());
            Assert.AreEqual(batch[0].LogValue, records[0].Payload.LogValue);

            records = logs.Dequeue(2);
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual(0, logs.Count());

            Assert.AreEqual(batch[1].LogValue, records[0].Payload.LogValue);
            Assert.AreEqual(batch[2].LogValue, records[1].Payload.LogValue);

            records = logs.Dequeue(2);
            Assert.AreEqual(0, records.Count);
        }

        [TestMethod]
        public void CustomOrder()
        {
            var logs = CreateQueue<CustomRecord>();
            logs.SetOrder((x) => { return x.Payload.Timestamp; });

            var batch = SampleData.GetCustomRecords();
            var ordered = batch.OrderBy(x => x.Timestamp);

            logs.Enqueue(batch);

            foreach (var record in ordered)
            {
                var records = logs.Dequeue(1);
                Assert.AreEqual(1, records.Count);
                Assert.AreEqual(record.LogValue, records[0].Payload.LogValue);
            }
        }
    }
}
