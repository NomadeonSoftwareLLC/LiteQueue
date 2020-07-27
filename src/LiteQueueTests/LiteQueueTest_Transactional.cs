/* Copyright 2018 by Nomadeon Software LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using LiteDB;
using LiteQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LiteQueueTests
{
    [TestClass]
    public class LiteQueueTest_Transactional
    {
        LiteDatabase _db;
        const string _collectionName = "transactionaltestcollection";

        LiteQueue<T> CreateQueue<T>()
        {
            var logCollection = _db.GetCollection<QueueEntry<T>>(_collectionName);
            var logs = new LiteQueue<T>(logCollection);
            return logs;
        }

        [TestInitialize]
        public void Init()
        {
            _db = new LiteDatabase("Filename=LiteQueueTest.db;connection=shared");
            _db.DropCollection(_collectionName);
        }

        [TestCleanup]
        public void Clean()
        {
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
        public void Enqueue()
        {
            var logs = CreateQueue<string>();

            logs.Enqueue("AddTest");

            Assert.AreEqual(1, logs.Count());
        }

        [TestMethod]
        public void EnqueueBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            Assert.AreEqual(3, logs.Count());
        }

        [TestMethod]
        public void Dequeue()
        {
            var logs = CreateQueue<string>();

            const string entry = "NextTest";
            logs.Enqueue(entry);

            var record = logs.Dequeue();
            Assert.IsTrue(record.IsCheckedOut);
            Assert.AreEqual(entry, record.Payload);
            Assert.AreEqual(1, logs.Count());

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
            Assert.AreEqual(3, logs.Count());

            records = logs.Dequeue(2);
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("b", records[0].Payload);
            Assert.AreEqual("c", records[1].Payload);
            Assert.AreEqual(3, logs.Count());

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
                var next = logs.Dequeue();
                Assert.AreEqual(i, next.Payload);
                logs.Commit(next);
            }

            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        public void CurrentCheckouts()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);
            var checkouts = logs.CurrentCheckouts();
            Assert.AreEqual(1, checkouts.Count);
            Assert.IsTrue(checkouts[0].IsCheckedOut);
            Assert.AreEqual(batch[0], checkouts[0].Payload);
        }

        [TestMethod]
        public void ResetOrphans()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(1);

            Assert.AreEqual(1, logs.CurrentCheckouts().Count);

            logs.ResetOrphans();

            Assert.AreEqual(0, logs.CurrentCheckouts().Count);
        }

        [TestMethod]
        public void Abort()
        {
            var logs = CreateQueue<string>();

            logs.Enqueue("AddTest");

            var record = logs.Dequeue();
            logs.Abort(record);

            Assert.AreEqual(1, logs.Count());
            Assert.AreEqual(0, logs.CurrentCheckouts().Count);
        }

        [TestMethod]
        public void AbortBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(3);
            logs.Abort(records);

            Assert.AreEqual(3, logs.Count());
            Assert.AreEqual(0, logs.CurrentCheckouts().Count);
        }

        [TestMethod]
        public void Commit()
        {
            var logs = CreateQueue<string>();

            logs.Enqueue("AddTest");

            var record = logs.Dequeue();
            logs.Commit(record);
            Assert.AreEqual(0, logs.Count());
        }

        [TestMethod]
        public void CommitBatch()
        {
            var logs = CreateQueue<string>();

            List<string> batch = new List<string>() { "a", "b", "c" };
            logs.Enqueue(batch);

            var records = logs.Dequeue(3);
            logs.Commit(records);

            Assert.AreEqual(0, logs.Count());
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

            var record1 = new CustomRecord()
            {
                Device = new DeviceLocation()
                {
                    LatitudeDegrees = 120,
                    LongitudeDegrees = 30
                },
                LogValue = "test",
                SensorReading = 2.2
            };
            var record2 = new CustomRecord()
            {
                Device = new DeviceLocation()
                {
                    LatitudeDegrees = 121,
                    LongitudeDegrees = 31
                },
                LogValue = "test2",
                SensorReading = 2.3
            };
            var record3 = new CustomRecord()
            {
                Device = new DeviceLocation()
                {
                    LatitudeDegrees = 122,
                    LongitudeDegrees = 32
                },
                LogValue = "test3",
                SensorReading = 2.4
            };

            var batch = new List<CustomRecord>() { record1, record2, record3 };

            logs.Enqueue(batch);

            var records = logs.Dequeue(1);
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(3, logs.Count());
            Assert.AreEqual(record1.LogValue, records[0].Payload.LogValue);
            Assert.AreEqual(1, logs.CurrentCheckouts().Count);

            logs.Abort(records);
            Assert.AreEqual(3, logs.Count());
            Assert.AreEqual(0, logs.CurrentCheckouts().Count);

            records = logs.Dequeue(1);
            logs.Commit(records);
            Assert.AreEqual(2, logs.Count());
            Assert.AreEqual(0, logs.CurrentCheckouts().Count);

            records = logs.Dequeue(2);
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual(2, logs.Count());
            Assert.AreEqual(2, logs.CurrentCheckouts().Count);

            Assert.AreEqual(record2.LogValue, records[0].Payload.LogValue);
            Assert.AreEqual(record3.LogValue, records[1].Payload.LogValue);

            logs.Commit(records);
            Assert.AreEqual(0, logs.Count());
            Assert.AreEqual(0, logs.CurrentCheckouts().Count);

            records = logs.Dequeue(2);
            Assert.AreEqual(0, records.Count);
        }
    }
 }
