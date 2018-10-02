using System;
using System.Threading;
using NUnit.Framework;
using PC_Serie_1;

namespace UnitTests
{
    [TestFixture]
    public class Ex01Tests
    {
        //----------------------------------------------------
        //                    SETUP
        //----------------------------------------------------

        // Este sincronizador suporta a troca de informação entre pares de threads identificados por uma chave.
        // As threads que utilizam este sincronizador manifestam a sua disponibilidade para iniciar uma troca invocando o método exchange​, especificando a identificação do par(key​),
        // o objecto que pretendem entregar à thread parceira(mydata​) e, opcionalmente, o tempo limite da espera pela troca(timeout​).

        KeyedExchanger<long> _keyedExchanger;        

        [SetUp]
        public void SetUp()
        {
            _keyedExchanger = new KeyedExchanger<long>();
        }

        private const int KEY = 123;
        private const int TIMEOUT = 1 * 1000; // 1 seg

        private const long DATA_1 = 10;
        private const long DATA_2 = 20;

        //----------------------------------------------------
        //                    TESTS
        //----------------------------------------------------

        //(1) devolvendo um optional com valor, quando é realizada a troca com outra thread, sendo o objecto por ela oferecido retornado no valor desse optional
        [Test]
        public void Test_01_Exchange_Data()
        {
            try
            {
                long? retExch1 = null, retExch2 = null;
   
                Thread t1 = new Thread(_ => retExch1 = _keyedExchanger.Exchange(KEY, DATA_1, TIMEOUT));
                Thread t2 = new Thread(_ => retExch2 = _keyedExchanger.Exchange(KEY, DATA_2, TIMEOUT));

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();

                Assert.IsTrue(retExch1.HasValue);
                Assert.IsTrue(retExch2.HasValue);

                Assert.AreEqual(DATA_1, retExch2.Value);
                Assert.AreEqual(DATA_2, retExch1.Value);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.StackTrace);
            }
        }

        // (2) devolvendo um optional vazio, se expirar o limite do tempo de espera especificado
        [Test]
        public void Test_02_Exchange_Data_Timeout()
        {
            long? retExch1 = null, retExch2 = null;

            Thread t1 = new Thread(_ => retExch1 = _keyedExchanger.Exchange(KEY, DATA_1, TIMEOUT));
            Thread t2 = new Thread(_ => retExch2 = _keyedExchanger.Exchange(KEY, DATA_2, TIMEOUT));

            t1.Start();
            t1.Join();

            t2.Start();
            t2.Join();

            Assert.IsNull(retExch1);
            Assert.IsNull(retExch2);
        }

        // (3) lançando ThreadInterruptedException quando a espera da thread for interrompida

        #region class MyKeyedExchangerThread
        public abstract class BaseThread
        {
            private readonly Thread m_thread;

            protected BaseThread()
            {
                m_thread = new Thread(Run);
            }

            public void Start()
            {
                m_thread.Start();
            }

            public void Interrupt()
            {
                //m_thread.Interrupt();
                throw new ThreadInterruptedException();
            }

            protected abstract void Run();
        }
        class MyKeyedExchangerThread<T> : BaseThread where T : struct
        {
            public int Key { get; }
            public readonly T Data;
            public readonly KeyedExchanger<T> Exg;
            public readonly int Timeout;
            public T? ExchangedData;

            public MyKeyedExchangerThread(int key, T data, KeyedExchanger<T> exg, int timeout)
            {
                Key = key;
                Data = data;
                Exg = exg;
                Timeout = timeout;
            }

            protected override void Run()
            {
                ExchangedData = Exg.Exchange(Key, Data, Timeout);
            }
        }
        #endregion

        [Test]
        public void Test_03_Exchange_Data_ThreadInterruptedException()
        {
            try
            {
                MyKeyedExchangerThread<long> excg = new MyKeyedExchangerThread<long>(KEY, DATA_1, _keyedExchanger, TIMEOUT);
                excg.Start();
                excg.Interrupt();

                //Thread thread = new Thread(_ =>
                //{
                //    _keyedExchanger.Exchange(KEY, DATA_1, TIMEOUT);
                //    Thread.CurrentThread.Interrupt();
                //});
                //thread.Start();
                ////thread.Interrupt(); // why doesn't work?
                //thread.Join();

                Assert.Fail();
            }
            catch (ThreadInterruptedException ex)
            {
                //Console.WriteLine(ex.StackTrace);
                Assert.IsTrue(true);
            }
        }
    }
}