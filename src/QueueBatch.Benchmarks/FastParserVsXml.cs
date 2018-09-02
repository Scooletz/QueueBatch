using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using QueueBatch.Impl.Queues;

namespace QueueBatch.Benchmarks
{
    public class FastParserVsXml
    {
        readonly byte[] payload;

        public FastParserVsXml()
        {
            payload = Encoding.UTF8.GetBytes(
                @"﻿<?xml version=""1.0"" encoding=""utf-8""?>
            <QueueMessagesList>
              <QueueMessage>
                <MessageId>5974b586-0df3-4e2d-ad0c-18e3892bfca2</MessageId>
                <InsertionTime>Fri, 09 Oct 2009 21:04:30 GMT</InsertionTime>
                <ExpirationTime>Fri, 16 Oct 2009 21:04:30 GMT</ExpirationTime>
                <PopReceipt>YzQ4Yzg1MDItYTc0Ny00OWNjLTkxYTUtZGM0MDFiZDAwYzEw</PopReceipt>
                <TimeNextVisible>Fri, 09 Oct 2009 23:29:20 GMT</TimeNextVisible>
                <DequeueCount>1</DequeueCount>
                <MessageText>PHRlc3Q+dGhpcyBpcyBhIHRlc3QgbWVzc2FnZTwvdGVzdD4=</MessageText>
              </QueueMessage>
            </QueueMessagesList>");
        }

        [Benchmark]
        [MemoryDiagnoser]
        public QueueMessage[] Sdk_parser()
        {
            var stream = new MemoryStream(payload);
            return new GetMessagesResponse(stream).Messages.ToArray();
        }

        [Benchmark]
        [MemoryDiagnoser]
        public List<Message> QueueBatch_custom_parser()
        {
            return FastXmlAzureParser.ParseGetMessages(new Memory<byte>(payload));
        }
    }
}