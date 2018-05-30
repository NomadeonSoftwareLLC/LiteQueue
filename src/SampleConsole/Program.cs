/* Copyright 2018 by Nomadeon Software LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using LiteDB;
using LiteQueue;
using System;

namespace SampleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // LiteQueue depends on LiteDB. You can save other things to same database.
            using (var db = new LiteDatabase("Queue.db"))
            {
                // Creates a "logs" collection in LiteDB. You can also pass a user defined object.
                var logs = new LiteQueue<string>(db, "logs");

                // Recommended on startup to reset anything that was checked out but not committed or aborted. 
                // Or call CurrentCheckouts to inspect them and abort yourself. See github page for
                // notes regarding duplicate messages.
                logs.ResetOrhpans();

                // Adds record to queue
                logs.Enqueue("Test");

                // Get next item from queue. Marks it as checked out such that other threads that 
                // call Checkout will not see it - but does not remove it from the queue.
                var record = logs.Dequeue();

                try
                {
                    // Do something that may potentially fail, i.e. a network call
                    // ...

                    // Removes record from queue
                    logs.Commit(record);
                }
                catch
                {
                    // Returns the record to the queue
                    logs.Abort(record);
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}