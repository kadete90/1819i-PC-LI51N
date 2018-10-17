using System.Collections.Concurrent;
using System.Threading;
using Ex03;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Ex03Tests
    {
        private const int TIMEOUT = 1 * 1000; // 1 seg

        [Test]
        public void SendMessages()
        {
            MessageQueue<int> queue = new MessageQueue<int>();

            ConcurrentBag<int> sendedValues = new ConcurrentBag<int>();
            ConcurrentBag<int> receivedValues = new ConcurrentBag<int>();

            int NUMBER_OF_THREADS = 10;

            for (int i = 0; i < NUMBER_OF_THREADS; i++)
            {
                var currentIdx = i;
                new Thread(() =>
                {
                    var sendStatus = queue.send(currentIdx);

                    if (sendStatus.@await(TIMEOUT))
                    {
                        sendedValues.Add(currentIdx);
                        Assert.IsTrue(sendStatus.isSent());
                    }
                    else
                    {
                        Assert.IsTrue(false);
                    }
                }).Start();
            }

            for (int i = 0; i < NUMBER_OF_THREADS; i++)
            {
                new Thread(() => {
                    var val = queue.receive(TIMEOUT/2);
                    if (val.HasValue)
                    {
                        receivedValues.Add(val.Value);
                    }
                }).Start();
            }

            Thread.Sleep(TIMEOUT + 100);

            Assert.IsTrue(sendedValues.Count == NUMBER_OF_THREADS);
            Assert.IsTrue(receivedValues.Count == NUMBER_OF_THREADS);
        }

        [Test]
        public void TryCancelMessages()
        {
            MessageQueue<int> queue = new MessageQueue<int>();

            ConcurrentBag<int> sendedValues = new ConcurrentBag<int>();
            ConcurrentBag<int> receivedValues = new ConcurrentBag<int>();

            int NUMBER_OF_ITERATIONS = 10;

            for (int i = 0; i < NUMBER_OF_ITERATIONS; i++)
            {
                var currentIdx = i;
                new Thread(() => {
                    var sendStatus = queue.send(currentIdx);

                    if (currentIdx%2 == 0)
                    {
                        if (sendStatus.tryCancel())
                        {
                            return;
                        }
                    }

                    if (sendStatus.@await(TIMEOUT))
                    {
                        sendedValues.Add(currentIdx);
                        Assert.IsTrue(sendStatus.isSent());
                    }
                    else
                    {
                        Assert.IsTrue(false);
                    }
                }).Start();
            }

            for (int i = 0; i < NUMBER_OF_ITERATIONS; i++)
            {
                new Thread(() => {
                    var val = queue.receive(TIMEOUT/2);
                    if (val.HasValue)
                    {
                        receivedValues.Add(val.Value);
                    }
                }).Start();
            }

            Thread.Sleep(TIMEOUT + 100);

            Assert.IsTrue(sendedValues.Count != NUMBER_OF_ITERATIONS && sendedValues.Count > 0);
            Assert.IsTrue(receivedValues.Count != NUMBER_OF_ITERATIONS && receivedValues.Count > 0);
            Assert.IsTrue(sendedValues.Count == receivedValues.Count);
        }
    }
}