using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace QueueBatch.Impl.Queues
{
    class FastXmlAzureParser
    {
        static readonly QueueMessageNodes Nodes = new QueueMessageNodes();

        class QueueMessageNodes
        {
            public readonly XmlNodeStartend MessageId;
            public readonly XmlNodeStartend DequeueCount;
            public readonly XmlNodeStartend MessageText;
            public readonly XmlNodeStartend PopReceipt;
            public readonly XmlNodeStartend QueueMessage;

            public QueueMessageNodes()
            {
                MessageId = new XmlNodeStartend(nameof(MessageId));
                DequeueCount = new XmlNodeStartend(nameof(DequeueCount));
                PopReceipt = new XmlNodeStartend(nameof(PopReceipt));
                MessageText = new XmlNodeStartend(nameof(MessageText));
                QueueMessage = new XmlNodeStartend(nameof(QueueMessage));
            }
        }


        public static List<Message> ParseGetMessages(Memory<byte> payload)
        {
            // ﻿<?xml version="1.0" encoding="utf-8"?>
            //<QueueMessagesList>
            //  <QueueMessage>
            //    <MessageId>5974b586-0df3-4e2d-ad0c-18e3892bfca2</MessageId>
            //    <InsertionTime>Fri, 09 Oct 2009 21:04:30 GMT</InsertionTime>
            //    <ExpirationTime>Fri, 16 Oct 2009 21:04:30 GMT</ExpirationTime>
            //    <PopReceipt>YzQ4Yzg1MDItYTc0Ny00OWNjLTkxYTUtZGM0MDFiZDAwYzEw</PopReceipt>
            //    <TimeNextVisible>Fri, 09 Oct 2009 23:29:20 GMT</TimeNextVisible>
            //    <DequeueCount>1</DequeueCount>
            //    <MessageText>PHRlc3Q+dGhpcyBpcyBhIHRlc3QgbWVzc2FnZTwvdGVzdD4=</MessageText>
            //  </QueueMessage>
            //</QueueMessagesList>

            var queueMessageStart = Nodes.QueueMessage.Start.Span;
            var messageIdStart = Nodes.MessageId.Start.Span;
            var messageIdEnd = Nodes.MessageId.End.Span;
            var popReceiptStart = Nodes.PopReceipt.Start.Span;
            var popReceiptEnd = Nodes.PopReceipt.End.Span;
            var dequeueStart = Nodes.DequeueCount.Start.Span;
            var dequeueEnd = Nodes.DequeueCount.End.Span;
            var messageStart = Nodes.MessageText.Start.Span;
            var messageEnd = Nodes.MessageText.End.Span;

            var offset = 0;
            var span = payload.Span;

            var messages = new List<Message>();

            Memory<byte> message = default;
            Memory<byte> popReceipt = default;
            Memory<byte> dequeue = default;

            while (TryConsumeTag(ref span, ref offset, queueMessageStart))
            {
                var shouldContinue = TryExtractTag(ref span, ref offset, messageIdStart, messageIdEnd, payload, out var messageId) &&
                                 TryExtractTag(ref span, ref offset, popReceiptStart, popReceiptEnd, payload, out popReceipt) &&
                                 TryExtractTag(ref span, ref offset, dequeueStart, dequeueEnd, payload, out dequeue) &&
                                 TryExtractTag(ref span, ref offset, messageStart, messageEnd, payload, out message);

                if (!shouldContinue)
                {
                    break;
                }

                if (Base64.DecodeFromUtf8InPlace(message.Span, out var written) != OperationStatus.Done)
                {
                    break;
                }

                // rewrite decoded message back to  the buffer
                message = message.Slice(0, written);
                messages.Add(new Message(messageId.ToUtf8String(), message, popReceipt.ToUtf8String(), dequeue.ToUtf8Int32()));
            }

            return messages;
        }

        static bool TryConsumeTag(ref Span<byte> span, ref int offset, Span<byte> tagToSkip)
        {
            var index = span.IndexOf(tagToSkip);
            if (index < 0)
            {
                return false;
            }

            var skip = index + tagToSkip.Length;
            span = span.Slice(skip);
            offset += skip;
            return true;
        }

        static bool TryExtractTag(ref Span<byte> span, ref int offset, Span<byte> startingTag, Span<byte> endingTag, Memory<byte> original, out Memory<byte> value)
        {
            var start = span.IndexOf(startingTag);
            if (start < 0)
            {
                value = default;
                return false;
            }

            var end = span.IndexOf(endingTag);
            if (start < 0)
            {
                value = default;
                return false;
            }

            value = original.Slice(offset + start + startingTag.Length, end - start - startingTag.Length);

            var skip = end + endingTag.Length;
            span = span.Slice(skip);
            offset += skip;
            return true;
        }
    }

    readonly struct XmlNodeStartend
    {
        public readonly Memory<byte> Start;
        public readonly Memory<byte> End;

        public XmlNodeStartend(string name)
        {
            Start = Encoding.UTF8.GetBytes($"<{name}>");
            End = Encoding.UTF8.GetBytes($"</{name}>");
        }
    }
}