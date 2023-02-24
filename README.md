# LiteQueue
Lightweight, persisted, thread safe, (optionally) transactional, FIFO queue built on [LiteDB](https://github.com/mbdavid/litedb).

### Background

On unattended or embedded systems, it is often a requirement to reliably deliver messages even when the network is periodically down or the machine is power cycled. When using Windows, [Microsoft Message Queuing (MSMQ)](https://en.wikipedia.org/wiki/Microsoft_Message_Queuing) is an old standby for queuing and can be [called](https://docs.microsoft.com/en-us/dotnet/api/system.messaging.messagequeue?view=netframework-4.7.1) from .NET code. But what if we want to use .NET Core and be OS portable?

One option is to install something like [ZeroMQ](https://en.wikipedia.org/wiki/ZeroMQ) or [RabbitMQ](https://en.wikipedia.org/wiki/RabbitMQ), but these are heavyweight for a client side queue on a single machine. [Queues.io](http://queues.io/) has a great list with some lighter options, but I did not find any with a nice .NET Standard client library.

So, it was time to explore creating one. It was not necessary to fully re-invent this wheel. In particular, I did not want to roll my own persistence layer. I came across [LiteDB](http://www.litedb.org/), an embedded NoSQL database specifically for .NET. It behaved quite nicely wrapped within queue logic, which I call LiteQueue.

### Nuget

```Install-Package LiteQueue```

### LiteQueue

LiteQueue provides:

- A persisted FIFO queue.
- Transactions (optionally). More on this below.
- API not too dissimilar from MSMQ for easy porting of legacy code. I use the method name `Dequeue` instead of `Receive` since this is intended to be a local queue.
- Thread safety. Multiple threads can add and remove from the queue.
- Portability via .NET Standard library.
- Storage of both primitives and user defined objects.
- Batch methods.
- Performance is limited by the constraints as LiteDB - recommended for use only on client machines or services with small loads. 
- MIT license, same as LiteDB

### Transactions

By default LiteQueue uses transactional logic. In this mode, `Dequeue` will flag an item as checked out but not remove it from the queue. You should call `Commit` (which fully removes the item) or `Abort` (which undoes checkout) after processing the retrieved item. Any other calls to `Dequeue` (on same or different threads) will not see items already checked out.

To turn transactional logic off, set transactional to false in the constructor. Some methods like `Commit` and `Abort` will throw `InvalidOperationException` if the queue is not transactional. I debated this as it makes it annoying to switch between the two modes, but I want to fail safe if you are trying to use transactional logic when the queue is not in that mode.

### Threading

If accessing the same queue from multiple threads, each thread must reference the same LiteQueue instance to ensure correct locking. If you encounter the following exception, suspect a violation of this rule as the cause:
```
SynchronizationLockException: Object synchronization method was called from an unsynchronized block of code.
```
This is a limitation of LiteDB after version 5.08.

### Message Duplication
Using a queue such as this to send messages to another system, there are two ways in which the receiver could see a duplicate message. The first is when the receiver gets the message, commits it, and sends its ACK but the sender fails to see the ACK (think cellular network). Your code will timeout and logic will trigger a resend. The other possibility is that you receive the final ACK but you get halted (process crash, power cycle) before you can remove the message from your local queue.

LiteQueue provides some help in the second situation (see `CurrentCheckouts` and `ResetOrphans`), but fundamentally de-duplication will require your receiver to identify and suppress duplicates. You will need to include a unique message identifier (such as GUID) in your message. If you have huge volumes, also consider a timestamp and de-duplication window so your receiver does not need to remember a complete history.

For further reading:

* [Two Generals' Problem](https://en.wikipedia.org/wiki/Two_Generals%27_Problem)
* [Duplicate detection in Azure](https://docs.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection)
* [Duplication detection in Segment](https://segment.com/blog/exactly-once-delivery/) (see also [criticisms](https://news.ycombinator.com/item?id=14664405))

### Code Example

Here's a quick start C# code snippet using transactional logic. See the unit tests for more usage.

```csharp
// LiteQueue depends on LiteDB. You can save other things to same database.
using (var db = new LiteDatabase("Queue.db"))
{
	// Creates a "logs" collection in LiteDB. You can also pass a user defined object.
	var logs = new LiteQueue<string>(db, "logs");

	// Recommended on startup to reset anything that was checked out but not committed or aborted. 
	// Or call CurrentCheckouts to inspect them and abort yourself. See github page for
	// notes regarding duplicate messages.
	logs.ResetOrphans();

	// Adds record to queue
	logs.Enqueue("Test");

	// Get next item from queue. Marks it as checked out such that other threads that 
	// call Checkout will not see it - but does not remove it from the queue.
	var record = logs.Dequeue();

	try
	{
		// Do something that may fail, i.e. a network call
		// record.Payload contains the original string "Test"

		// Removes record from queue
		logs.Commit(record);
	}
	catch
	{
		// Returns the record to the queue
		logs.Abort(record);
	}
}
```

### License

[MIT](https://github.com/NomadeonSoftwareLLC/LiteQueue/blob/master/LICENSE)

Copyright (C) by Nomadeon Software LLC
