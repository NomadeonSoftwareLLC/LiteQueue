/* Copyright 2024 by Nomadeon LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using LiteDB;
using LiteQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiteQueueTests
{
    [TestClass]
    public class LiteQueueTest_Threaded
    {
        LiteDatabase _db;
        LiteQueue<int> _queue;

        const string _collectionName = "threadedtestcollection";

        /// <summary>
        /// How many records created by each producer thread
        /// </summary>
        const int _recordsToProduce = 100;

        /// <summary>
        /// Monotomically increasing value shared across producers
        /// </summary>
        int _producerCounter = 0;

        /// <summary>
        /// Shared by all consumers
        /// </summary>
        HashSet<int> _consumedRecords = new HashSet<int>();

        /// <summary>
        /// Consumers keep running until false
        /// </summary>
        bool _keepRunning = true;

        bool _consumerFailed = false;

        [TestInitialize]
        public void Init()
        {
            _db = new LiteDatabase("Filename=LiteQueueTest.db;connection=shared");
            _db.DropCollection(_collectionName);

            _queue = new LiteQueue<int>(_db, _collectionName);
        }

        [TestCleanup]
        public void Clean()
        {
            _db.DropCollection(_collectionName);
        }

        [TestMethod]
        public void Single()
        {
            Action producer = delegate () { Producer(_queue); };
            Action consumer = delegate () { Consumer(_queue); };
            RunTasks(producer, consumer, producerCount: 1, consumerCount: 1);
        }

        [TestMethod]
        public void MultipleProducers()
        {
            Action producer = delegate () { Producer(_queue); };
            Action consumer = delegate () { Consumer(_queue); };
            RunTasks(producer, consumer, producerCount: 10 , consumerCount: 1);
        }


        [TestMethod]
        public void MultipleConsumers()
        {
            Action producer = delegate () { Producer(_queue); };
            Action consumer = delegate () { Consumer(_queue); };
            RunTasks(producer, consumer, producerCount: 1, consumerCount: 10);
        }

        [TestMethod]
        public void MultipleProducersMultipleConsumers()
        {
            Action producer = delegate () { Producer(_queue); };
            Action consumer = delegate () { Consumer(_queue); };
            RunTasks(producer, consumer, producerCount: 10, consumerCount: 10);
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateException))]
        public void Duplicate()
        {
            Action producer = delegate () { BadProducer(_queue); };
            Action consumer = delegate () { Consumer(_queue); };
            RunTasks(producer, consumer, producerCount: 1, consumerCount: 1);
        }

        /// <summary>
        /// Runs a multi-threaded producer/consumer test
        /// </summary>
        /// <param name="producerCount"># of producer threads to run</param>
        /// <param name="consumerCount"># of consumer threads to run</param>
        /// <param name="producer">Function to run for each producer</param>
        /// <param name="consumer">Function to run for each consumer</param>
        void RunTasks(Action producer, Action consumer, int producerCount, int consumerCount)
        {
            List<Task> producers = new List<Task>();
            for (int i = 0; i < producerCount; i++)
            {
                Task producerTask = new Task(producer);
                producers.Add(producerTask);
                producerTask.Start();
            }

            List<Task> consumers = new List<Task>();
            for (int i = 0; i < consumerCount; i++)
            {
                Task consumerTask = new Task(consumer);
                consumers.Add(consumerTask);
                consumerTask.Start();
            }

            Task.WaitAll(producers.ToArray());
            WaitForEmptyQueue(_queue);

            _keepRunning = false;
            try
            {
                Task.WaitAll(consumers.ToArray());
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }

            VerifyAllConsumed(producerCount);
        }

        void Producer(LiteQueue<int> queue)
        {
            for (int i = 0; i < _recordsToProduce; i++)
            {
                int next = Interlocked.Increment(ref _producerCounter);

                queue.Enqueue(next);
            }
        }

        void BadProducer(LiteQueue<int> queue)
        {
            for (int i = 0; i < _recordsToProduce; i++)
            {
                int next = 1; // Should cause DuplicateException in consumer

                queue.Enqueue(next);
            }
        }

        void Consumer(LiteQueue<int> queue)
        {
            try
            {
                while (_keepRunning)
                {
                    var entry = queue.Dequeue();
                    if (entry != null)
                    {
                        if (!_consumedRecords.Add(entry.Payload))
                        {
                            throw new DuplicateException(entry.Payload);
                        }
                        queue.Commit(entry);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch
            {
                _consumerFailed = true;
                throw;
            }
        }

        void WaitForEmptyQueue(LiteQueue<int> queue)
        {
            while (queue.Count() > 0 && !_consumerFailed)
            {
                Thread.Sleep(5);
            }
        }

        void VerifyAllConsumed(int producerThreadCount)
        {
            int expected = producerThreadCount * _recordsToProduce;
            Assert.AreEqual(expected, _consumedRecords.Count);
        }
    }
 }
