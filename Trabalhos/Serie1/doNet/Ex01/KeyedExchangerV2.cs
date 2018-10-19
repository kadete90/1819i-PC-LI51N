using System;
using System.Collections.Generic;
using System.Linq;
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

    public class KeyedExchangerV2<T> where T: struct
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

        private readonly List<DataHolder> _dataHolders; // execution delegation 

        public long HoldersCount() { lock (ExchangerLock) return _dataHolders.Count; }

        public KeyedExchangerV2()
        {

            this.ExchangerLock = new object();
            this._dataHolders = new List<DataHolder>();
        }

        public T? Exchange(int key, T myData, int timeout) //throws InterruptedException;
        {
            lock (ExchangerLock)
            {
                var existentHolder = _dataHolders.FirstOrDefault(d => d.Key == key);

                if (existentHolder != null)
                {
                    if (existentHolder.Signal)
                    {
                        //a third thread trying to switch data ??
                        throw new InvalidOperationException();
                    }

                    // switch data, signal and pulse all for the exchanged thread return the switched value
                    var dataToRet = existentHolder.Data;

                    existentHolder.Data = myData;
                    existentHolder.Signal = true;

                    Monitor.PulseAll(ExchangerLock);
                    
                    //(1) devolvendo um optional com valor, quando é realizada a troca com outra thread
                    return dataToRet;
                }

                TimeoutHolder th = new TimeoutHolder(timeout);

                try
                {
                    existentHolder = new DataHolder(key, myData);
                    _dataHolders.Add(existentHolder);

                    do
                    {
                        Monitor.Wait(ExchangerLock, timeout);

                        if (existentHolder.Signal)
                        {
                            // (1) devolvendo um optional com valor, quando é realizada a troca com outra thread
                            _dataHolders.Remove(existentHolder);
                            return existentHolder.Data;
                        }

                        if (th.Timeout)
                        {
                            // (2) devolvendo um optional vazio, se expirar o limite do tempo de espera especificado,
                            _dataHolders.Remove(existentHolder);
                            return null;
                        }

                    } while (true);
                }
                catch (ThreadInterruptedException)
                {
                    // (3) lançando ThreadInterruptedException quando a espera da thread for interrompida.
                    if (existentHolder != null && existentHolder.Signal)
                    {
                        Thread.CurrentThread.Interrupt();
                        _dataHolders.Remove(existentHolder);

                        return existentHolder.Data;
                    }

                    _dataHolders.Remove(existentHolder);
                    throw;
                }
            }
        }
    }
}
