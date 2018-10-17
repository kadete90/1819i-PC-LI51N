using System.Collections.Generic;
using System.Threading;
using SyncUtils;

namespace Ex03
{
    // Este sincronizador permite a comunicação entre threads produtoras e threads consumidoras.

    // A implementação do sincronizador deve optimizar o número de comutações de thread que ocorrem nas várias circunstâncias.

    public class Request<T> where T : struct
    {
        public T? Msg { get; set; }
        public bool Done { get; set; }

        public Request(T? msg, bool done)
        {
            Msg = msg;
            Done = done;
        }
    }

    public class MessageQueue<T> where T : struct 
    {
        private readonly LinkedList<Request<T>> _producers = new LinkedList<Request<T>>();
        private readonly LinkedList<Request<T>> _consumers = new LinkedList<Request<T>>();
        private object MsgQueueLock { get; }

        public MessageQueue()
        {
            MsgQueueLock = new object();
        }

        #region class SendStatusImpl
        class MessageHandler : SendStatus
        {
            private readonly LinkedList<Request<T>> _producers;
            private readonly LinkedList<Request<T>> _consumers;

            private LinkedListNode<Request<T>> Node { get; }

            public MessageHandler(LinkedList<Request<T>> consumers, LinkedList<Request<T>> producers, LinkedListNode<Request<T>> node)
            {
                _consumers = consumers;
                _producers = producers;
                Node = node;
            }

            // O método isSent​ retorna true​ se a mensagem já foi entregue a outra thread, false​ em caso contrário.
            public bool isSent()
            {
                return Node.Value.Done;
            }

            // O método tryCancel tenta remover a mensagem da fila, retornando o sucesso dessa remoção (a remoção pode já não ser possível).
            public bool tryCancel()
            {
                if (Node.Value.Done)
                {
                    return false;
                }

                if (!_producers.Contains(Node.Value))
                {
                    return false;
                }

                try
                {
                    _producers.Remove(Node);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // O método await sincroniza com a entrega da mensagem:
                //(a) devolvendo true quando a mensagem for recebida por uma thread consumidora;
                //(b) devolvendo false se expirar o limite especificado para o tempo de espera, ou;
                //(c) lançando InterruptedException​, se o bloqueio da thread for interrompido.

            public bool await(int timeout)
            {
                lock (this)
                {
                    if (_consumers.Count > 0)
                    {
                        _consumers.Last.Value.Msg = Node.Value.Msg;
                        _consumers.Last.Value.Done = true;
                        _consumers.RemoveLast();
                        Monitor.PulseAll(this);
                        return true;
                    }

                    if (timeout == 0) { return false; }

                    TimeoutHolder th = new TimeoutHolder(timeout);

                    do
                    {
                        try
                        {
                            Monitor.Wait(this, th.Value);

                            if (Node.Value.Done)
                            {
                                return true;
                            }

                            if (th.Timeout)
                            {
                                _producers.Remove(Node);
                                return false;
                            }
                        }
                        catch (ThreadInterruptedException)
                        {
                            if (Node.Value.Done)
                            {
                                Thread.CurrentThread.Interrupt();
                                return true;
                            }
                            _producers.Remove(Node);
                            throw;
                        }
                    } while (true);
                }
            }
        }
        #endregion

        // A operação send entrega uma mensagem à fila(sentMsg​), e termina imediatamente retornando um objecto que implementa a interface SendStatus​.
        // Este objecto permite a sincronização com a entrega da respectiva mensagem a outra thread.

        public SendStatus send(T sentMsg)
        {
            lock (MsgQueueLock)
            {
                LinkedListNode<Request<T>> node = _producers.AddLast(new Request<T>(sentMsg, false));
                return new MessageHandler(_consumers, _producers, node);
            }
        }

        // O método receive permite receber uma mensagem da fila, e termina:
            //(a) devolvendo um optional com a mensagem, em caso de sucesso;
            //(b) devolvendo um optional vazio se expirar o limite especificado para o tempo de espera, ou;
            //(c) lançando InterruptedException​, se o bloqueio da thread for interrompido.

        public T? receive(int timeout) //throws InterruptedException;
        {
            lock (MsgQueueLock)
            {
                if (_producers.Count > 0)
                {
                    var toRet = _producers.First.Value;
                    _producers.First.Value.Done = true;
                    _producers.RemoveFirst();
                    Monitor.PulseAll(MsgQueueLock);
                    return toRet.Msg;
                }

                if (timeout == 0)
                {
                    return null;
                }

                TimeoutHolder th = new TimeoutHolder(timeout);

                LinkedListNode<Request<T>> req = _consumers.AddLast(new Request<T>(null, false));

                do
                {
                    try
                    {
                        Monitor.Wait(MsgQueueLock, th.Value);
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (req.Value.Done)
                        {
                            Thread.CurrentThread.Interrupt();
                            _consumers.Remove(req);
                            return req.Value.Msg;
                        }
                        _consumers.Remove(req);
                        throw;
                    }

                    if (req.Value.Done)
                    {
                        return req.Value.Msg;
                    }

                    if (th.Timeout)
                    {
                        _consumers.Remove(req);
                        return null;
                    }
                } while (true);
            }
        }
    }
}
