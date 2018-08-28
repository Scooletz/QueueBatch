using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using PublicApiGenerator;

namespace QueueBatch.Tests
{
    [TestFixture]
    public class ApiApprovals
    {
        [Test]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(IMessageBatch).Assembly);
            Approve(publicApi);
        }

        static void Approve(string publicApi,[CallerFilePath] string filePath = null)
        {
            var directory = Path.GetDirectoryName(filePath);
            var file = Path.Combine(directory, "approved_api.cs");
            if (File.Exists(file) == false)
            {
                File.WriteAllText(file, publicApi);
                return;
            }

            var approved = File.ReadAllText(file);
            Assert.AreEqual(approved, publicApi);
        }
    }
}