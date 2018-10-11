using System;
using System.Collections.Generic;
using System.Threading;
using SyncUtils;

namespace Ex03
{
    // Este sincronizador permite a comunicação entre threads produtoras e threads consumidoras.
  
    // A implementação do sincronizador deve optimizar o número de comutações de thread que ocorrem nas várias circunstâncias.

    public class MessageQueue<T> where T : struct 
    {
        public object _queueLock { get; }

        public readonly LinkedList<T> _queue = new LinkedList<T>();

        public MessageQueue()
        {
            _queueLock = new object();
        }

        #region class SendStatusImpl
        class MessageHandler : SendStatus
        {
            private readonly MessageQueue<T> _messageQueue;

            private LinkedListNode<T> _node { get; }

            public bool msgSend { get; set; }

            public MessageHandler(MessageQueue<T> messageQueue, LinkedListNode<T> node)
            {
                _messageQueue = messageQueue;
                this._node = node;
                msgSend = false;
            }

            // O método isSent​ retorna true​ se a mensagem já foi entregue a outra thread, false​ em caso contrário.
            public bool isSent()
            {
                return msgSend;
            }

            // O método tryCancel tenta remover a mensagem da fila, retornando o sucesso dessa remoção (a remoção pode já não ser possível).
            public bool tryCancel()
            {
                if (msgSend)
                {
                    return false;
                }

                if (_messageQueue._queue.Contains(_node.Value))
                {
                    try
                    {
                        _messageQueue._queue.Remove(_node);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                return false;
            }

            // O método await sincroniza com a entrega da mensagem:
                //(a) devolvendo true quando a mensagem for recebida por uma thread consumidora;
                //(b) devolvendo false se expirar o limite especificado para o tempo de espera, ou;
                //(c) lançando InterruptedException​, se o bloqueio da thread for interrompido.

            public bool await(int timeout)
            {
                Monitor.Enter(this);

                try
                {
                    do
                    {
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
                finally
                {
                    Monitor.Exit(this);
                }
            }
        }
        #endregion

        // A operação send entrega uma mensagem à fila(sentMsg​), e termina imediatamente retornando um objecto que implementa a interface SendStatus​.
        // Este objecto permite a sincronização com a entrega da respectiva mensagem a outra thread.

        public SendStatus send(T sentMsg)
        {
            lock (_queueLock)
            {
                var node = _queue.AddLast(sentMsg);
                return new MessageHandler(this, node);
            }
        }

        // O método receive permite receber uma mensagem da fila, e termina:
            //(a) devolvendo um optional com a mensagem, em caso de sucesso;
            //(b) devolvendo um optional vazio se expirar o limite especificado para o tempo de espera, ou;
            //(c) lançando InterruptedException​, se o bloqueio da thread for interrompido.

        public T? receive(int timeout) //throws InterruptedException;
        {
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                {
                    _queue.RemoveFirst();
                    var toRet = _queue.First.Value;
                    return toRet;
                }
            }

            if (msgHandler != null)
            {
                return msgHandler.@await(timeout) 
                    ? (T?)msgHandler.msg 
                    : null;
            }
            

            lock (_queueLock)
            {
                try
                {
                    do
                    {
                        TimeoutHolder th = new TimeoutHolder(timeout);

                        Monitor.Wait(_queueLock, timeout);

                        if (_queue.Count > 0)
                        {
                            msgHandler = _queue.Dequeue();
                            break;
                        }

                        if (th.Timeout)
                        {
                            // TODO STUFF
                            return null;
                        }

                    } while (true);
                }
                catch (ThreadInterruptedException ex)
                {
                    // TODO STUFF

                    throw;
                }
            }
           
            return msgHandler != null && msgHandler.@await(timeout) 
                ? (T?)msgHandler.msg 
                : null;
        }
    }
}
