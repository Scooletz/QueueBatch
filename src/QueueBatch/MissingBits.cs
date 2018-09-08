using System;
using System.Text;

namespace QueueBatch
{
    static class MissingBits
    {
        public static string ToUtf8String(this Memory<byte> messageId)
        {
            var sb = new StringBuilder(messageId.Length);
            var messageIdSpan = messageId.Span;
            for (var i = 0; i < messageId.Length; i++)
            {
                sb.Append((char)messageIdSpan[i]);
            }

            return sb.ToString();
        }

        public static int ToUtf8Int32(this Memory<byte> messageId)
        {
            if (messageId.Length == 1)
            {
                var ch = (char)messageId.Span[0];
                switch (ch)
                {
                    case '0': return 0;
                    case '1': return 1;
                    case '2': return 2;
                    case '3': return 3;
                    case '4': return 4;
                    case '5': return 5;
                    case '6': return 6;
                    case '7': return 7;
                    case '8': return 8;
                    case '9': return 9;
                }
            }

            return SlowToUtf8Int32(messageId);
        }

        static int SlowToUtf8Int32(in Memory<byte> messageId) => int.Parse(messageId.ToUtf8String());
    }
}