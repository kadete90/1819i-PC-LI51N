﻿using System;
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

        KeyedExchangerV2<long> _keyedExchanger;        

        [SetUp]
        public void SetUp()
        {
            _keyedExchanger = new KeyedExchangerV2<long>();
        }

        private const int KEY = 123;
        private const int TIMEOUT = 100; // 0.1 seg

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

        [Test]
        public void Test_01_Exchange_Data_v2()
        {
            try
            {
                long? retExch1 = null, retExch2 = null, retExch3 = null;

                Assert.IsTrue(_keyedExchanger.HoldersCount() == 0);

                Thread t1 = new Thread(_ =>
                {
                    retExch1 = _keyedExchanger.Exchange(KEY, DATA_1, TIMEOUT);
                    Assert.IsTrue(_keyedExchanger.HoldersCount() == 1);
                });
                Thread t2 = new Thread(_ =>
                {
                    retExch2 = _keyedExchanger.Exchange(KEY - 5, DATA_1, TIMEOUT);
                    Assert.IsTrue(_keyedExchanger.HoldersCount() == 2);
                });
                Thread t3 = new Thread(_ =>
                {
                    retExch3 = _keyedExchanger.Exchange(KEY, DATA_2, TIMEOUT);
                    Assert.IsTrue(_keyedExchanger.HoldersCount() == 1);
                });

                t1.Start();
                t2.Start();
                t3.Start();
                
                t1.Join();
                t2.Join();
                t3.Join();

                Assert.IsTrue(_keyedExchanger.HoldersCount() == 0);
                Assert.IsFalse(retExch2.HasValue);

                Assert.IsTrue(retExch1.HasValue);
                Assert.IsTrue(retExch3.HasValue);

                Assert.AreEqual(DATA_1, retExch3.Value);
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
        [Test]
        public void Test_03_Exchange_Data_ThreadInterruptedException()
        {
            bool interrupted = false;

            Thread thread = new Thread(_ =>
            {
                try
                {
                    _keyedExchanger.Exchange(KEY, DATA_1, TIMEOUT * 2);
                }
                catch (ThreadInterruptedException e)
                {
                    interrupted = true;
                }
            });
            thread.Start();

            Thread t2 = new Thread(() =>
            {
                Thread.Sleep(TIMEOUT);
                thread.Interrupt();
            });

            t2.Start();

            thread.Join();
            t2.Join();

            Assert.IsTrue(interrupted);
        }
    }
}