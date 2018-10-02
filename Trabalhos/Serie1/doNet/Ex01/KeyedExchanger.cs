using System;
using System.Threading;
using SyncUtils;

namespace PC_Serie_1
{
    // Este sincronizador suporta a troca de informação entre pares de threads identificados por uma chave.
    // As threads que utilizam este sincronizador manifestam a sua disponibilidade para iniciar uma troca invocando o método exchange​, especificando a identificação do par(key​),
    // o objecto que pretendem entregar à thread parceira(mydata​) e, opcionalmente, o tempo limite da espera pela troca(timeout​).

    // O método exchange termina:
        // (1) devolvendo um optional com valor, quando é realizada a troca com outra thread, sendo o objecto por ela oferecido retornado no valor desse optional;
        // (2) devolvendo um optional vazio, se expirar o limite do tempo de espera especificado, ou;
        // (3) lançando ThreadInterruptedException quando a espera da thread for interrompida.

    public class KeyedExchanger<T> where T: struct 
    {
        public class DataHolder
        {
            public DataHolder(int key, T data) {
                Key = key;
                Data = data;
                Signal = false;
            }

            public int Key;
            public T Data;
            public bool Signal;
        }

        private object ExchangerLock { get; }

        private DataHolder exchangeHolder; // execution delegation 

        public KeyedExchanger()
        {
            this.ExchangerLock = new object();
            this.exchangeHolder = null;
        }

        public T? Exchange(int key, T mydata, int timeout) //throws InterruptedException;
        {
            lock (ExchangerLock)
            {
                if (exchangeHolder != null && key == exchangeHolder.Key)
                {
                    if (exchangeHolder.Signal)
                    {
                        //a third thread trying to switch data ??
                        throw new InvalidOperationException();
                    }

                    // switch data, signal and pulse all for the exchanged thread return the switched value
                    var dataToRet = exchangeHolder.Data;

                    exchangeHolder.Data = mydata;
                    exchangeHolder.Signal = true;

                    Monitor.PulseAll(ExchangerLock);

                    //(1) devolvendo um optional com valor, quando é realizada a troca com outra thread
                    return dataToRet;
                }

                TimeoutHolder th = new TimeoutHolder(timeout);

                try
                {
                    do
                    {
                        exchangeHolder = new DataHolder(key, mydata);

                        Monitor.Wait(ExchangerLock, timeout);

                        if (exchangeHolder.Signal)
                        {
                            // (1) devolvendo um optional com valor, quando é realizada a troca com outra thread
                            return exchangeHolder.Data;
                        }

                        if (th.Timeout)
                        {
                            // (2) devolvendo um optional vazio, se expirar o limite do tempo de espera especificado,
                            exchangeHolder = null;
                            return null;
                        }

                    } while (true);
                }
                catch (ThreadInterruptedException ex)
                {
                    // (3) lançando ThreadInterruptedException quando a espera da thread for interrompida.
                    exchangeHolder = null;
                    throw;
                }
            }
        }
    }
}
