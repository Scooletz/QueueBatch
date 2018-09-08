using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Tests
{
    public class ParserTests
    {
        [TestCaseSource(nameof(GetCases))]
        public void GetMessages(Messages messagesObj)
        {
            var data = messagesObj.Data;
            var bytes = BuildXmlBytes(data);
            var messages = FastXmlAzureParser.ParseGetMessages(bytes);

            Assert.AreEqual(data.Length, messages.Count);

            for (var i = 0; i < data.Length; i++)
            {
                var messageData = data[i];
                var msg = messages[i];

                var expectedMessagePayload = Convert.FromBase64String(messageData.MessageText);

                Assert.AreEqual(messageData.Id, msg.Id);
                CollectionAssert.AreEqual(expectedMessagePayload, msg.Payload.ToArray(), "Payload is wrongly deserialized");
                Assert.AreEqual(messageData.DequeueCount, msg.DequeueCount);
                Assert.AreEqual(messageData.PopReceipt, msg.PopReceipt);
            }
        }

        public static IEnumerable<TestCaseData> GetCases()
        {
            yield return new TestCaseData(new Messages(

                new MessageData
                {
                    Id = "5974b586-0df3-4e2d-ad0c-18e3892bfca2",
                    PopReceipt = "YzQ4Yzg1MDItYTc0Ny00OWNjLTkxYTUtZGM0MDFiZDAwYzEw",
                    DequeueCount = 3,
                    MessageText = "PHRlc3Q+dGhpcyBpcyBhIHRlc3QgbWVzc2FnZTwvdGVzdD4="
                }
            )).SetName("Simple message");

            yield return new TestCaseData(new Messages(

                new MessageData
                {
                    Id = "id",
                    PopReceipt = "pop",
                    DequeueCount = 124324,
                    MessageText = "text"
                }
            )).SetName("Big dequeue count");

            yield return new TestCaseData(new Messages(

                new MessageData
                {
                    Id = "id1",
                    PopReceipt = "pop1",
                    DequeueCount = 1,
                    MessageText = "0001"
                },

                new MessageData
                {
                    Id = "id2",
                    PopReceipt = "pop2",
                    DequeueCount = 2,
                    MessageText = "0002"
                }
            )).SetName("Multiple");
        }

        static byte[] BuildXmlBytes(IEnumerable<MessageData> data)
        {
            const string payloadStart = @"﻿<?xml version=""1.0"" encoding=""utf-8""?>
<QueueMessagesList>
  ";
            const string payloadEnd = "</QueueMessagesList>";

            var sb = new StringBuilder();
            sb.Append(payloadStart);

            foreach (var messageData in data)
            {
                sb.Append(BuildQueueMessage(messageData.Id, messageData.PopReceipt, messageData.DequeueCount,
                    messageData.MessageText));
            }

            sb.Append(payloadEnd);

            var xml = sb.ToString();
            var bytes = Encoding.UTF8.GetBytes(xml);
            return bytes;
        }

        static string BuildQueueMessage(string messageId, string popReceipt, int dequeueCount, object messageText)
        {
            return $@"<QueueMessage>
    <MessageId>{messageId}</MessageId>
    <InsertionTime>Fri, 09 Oct 2009 21:04:30 GMT</InsertionTime>
    <ExpirationTime>Fri, 16 Oct 2009 21:04:30 GMT</ExpirationTime>
    <PopReceipt>{popReceipt}</PopReceipt>
    <TimeNextVisible>Fri, 09 Oct 2009 23:29:20 GMT</TimeNextVisible>
    <DequeueCount>{dequeueCount}</DequeueCount>
    <MessageText>{messageText}</MessageText>
  </QueueMessage>
";
        }

        public class Messages
        {
            public MessageData[] Data;

            public Messages(params MessageData[] data)
            {
                Data = data;
            }

            public Messages(MessageData messageData)
            {
                Data = new[] { messageData };
            }
        }

        public class MessageData
        {
            public string Id;
            public string PopReceipt;
            public int DequeueCount;
            public string MessageText;
        }
    }
}