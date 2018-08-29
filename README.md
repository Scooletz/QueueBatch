![Icon](https://raw.githubusercontent.com/Scooletz/QueueBatch/dev/package_icon.png)

# QueueBatch

QueueBatch is an Azure Functions trigger providing ability to process Azure Storage Queue messages in batches.

## Why?

Azure Functions trigger for Azure Storage Queue obtains messages in batches but dispatches them in separate tasks. It's ok, if a function touches different resources. If a function does some processing and then appends to a single append blob it won't scale up nicely. The problems that might occur are: 
- issuing many IO transactions, 
- failing due to concurrency checks (hot resources)
- breaching throughput of a partition, etc. 

Accessing the same resource with high frequency might simply not work. With `QueueBatch`, you can address it, by processing all the messages from the batch in the same function, amortizing the cost of accessing other resources.

## Usage

To use `QueueBatch` in your Function application, you need to use a custom `IMessageBatch` parameter type to accept a batch and mark it with an appropriate attribute

```c#
public static void MyFunc([QueueBatchTrigger("myqueue")] IMessageBatch batch)
{
  // process messages
  foreach (var msg in batch.Messages)
  {
    // do something with payload
    DoSomething(msg.Payload);
  }

  // acknowledge processing
  batch.MarkAllAsProcessed ();
}
```

You can also acknowledge only some of the messages. The rest, will be retried in a similar manner to the regural `[QueueTrigger]`

```c#
public static void MyFunc([QueueBatchTrigger("myqueue")] IMessageBatch batch)
{
  // process messages
  foreach (var msg in batch.Messages)
  {
    // do something with payload
    if (DoSomething(msg.Payload))
    {
       // mark as processed only if successful
       batch.MarkAsProcessed (msg);
    }
  }
}
```

## Licensing

### QueueBatch

QueueBatch is licensed under the Apache License 2.0 license.

### [Azure Webjobs SDK](https://github.com/Azure/azure-webjobs-sdk) 

Azure Webjobs SDK is licensed under the MIT license as described [here](https://github.com/Azure/azure-webjobs-sdk/blob/dev/LICENSE.txt).
Azure Webjobs SDK sources are used and partially compiled into the QueueBatch distribution as allowed under the license terms found [here](https://github.com/Azure/azure-webjobs-sdk/blob/dev/LICENSE.txt).

## Icon

[Batch Download](https://thenounproject.com/term/cloud-batch-download/1035171/) designed by [Fatahillah](https://thenounproject.com/fatahillah/) from The Noun Project