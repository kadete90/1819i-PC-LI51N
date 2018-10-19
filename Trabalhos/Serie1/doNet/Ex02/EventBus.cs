using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ex02
{
    // Esta classe visa disponibilizar um mecanismo para que eventos dum sistema (e.g., utilizador registrou-se com sucesso, ligação à base de dados falhou)
    // sejam publicados para todos os subscritores interessados nesse tipo de evento.

    // Por exemplo o sistema pode ter um componente subscrito no evento de utilizador registrado com sucesso de forma a enviar-lhe um email de boas vindas.
    // A publicação dum evento consiste no envio de um objecto, designado por mensagem, para todos os subscritores registados no tipo desse objecto.

    public class EventBus
    {
        public enum BusState { Active, InShutDown, Terminated }
        private BusState State { get; set; }
        public BusState GetState() { lock (this) return State; }

        public class EventTypeHandler
        {
            //public LinkedList<List<subscriber, object>> BusMessage;
            //public int Waiters { get; set; }
            //public readonly object HandlerLock = new object();

            //public List<object> BusMessages;
            public ConcurrentBag<object> BusMessages;

            public EventTypeHandler(Type type)
            {
                //BusMessages = new List<object>();
                BusMessages = new ConcurrentBag<object>();
                Type = type;
            }

            public EventTypeHandler(Type type, object message)
            {
                BusMessages = new ConcurrentBag<object> {message};
                Type = type;
                BeingUsed = false;
            }

            public Type Type { get; }      

            public bool BeingUsed { get; set; }
        }

        private readonly object _eventBusLock = new object();
        private readonly int _maxPending;

        private readonly Dictionary<Type, EventTypeHandler> _handlers = new Dictionary<Type, EventTypeHandler>(); 

        public EventBus(int maxPending)
        {
            if (maxPending < 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            State = BusState.Active;
            _maxPending = maxPending;
        }

        // O método SubscribeEvent regista um handler para ser executado sempre que for publicada uma mensagem do tipo T​,
        // sendo o handler executado pela thread que procedeu ao respectivo registo com SubscribeEvent​.
        // Note-se que a chamada a este método é bloqueante, só retornando após um shutdown ou se a respectiva thread for interrompida.
        // O método SubscribeEvent deve lançar ThreadInterruptedException no caso da thread invocante ser interrompida enquanto estiver bloqueada ou durante a execução do handler.
        // Após a chamada ao método Shutdown​, todas as chamadas ao método SubscribeEvent deverão retornar após serem processadas todas as mensagens publicadas.

        public void SubscribeEvent<T>(Action<T> handler) where T : class
        {
            EventTypeHandler eventTypeHandler = new EventTypeHandler(typeof(T));

            lock (_eventBusLock)
            {
                if (State != BusState.Active)
                {
                    return;
                }

                if (_handlers.ContainsKey(typeof(T)))
                {
                    throw new InvalidOperationException("Ony can be one handler of each type");
                }

                _handlers.Add(typeof(T), eventTypeHandler);
            }

            do
            {
                List<T> messages = new List<T>();

                lock (_eventBusLock)
                {
                    try
                    {
                        if (State != BusState.Active)
                        {
                            _handlers.Remove(typeof(T));

                            if (!_handlers.Any())
                            {
                                State = BusState.Terminated;
                                Monitor.PulseAll(_eventBusLock);
                                return;
                            }
                        }

                        do
                        {
                            if (eventTypeHandler.BusMessages.IsEmpty)
                            {
                                Monitor.Wait(_eventBusLock);
                            }
                            else
                            {
                                while (eventTypeHandler.BusMessages.TryTake(out object msg))
                                {
                                    T val = (T)Convert.ChangeType(msg, typeof(T));
                                    messages.Add(val);
                                }

                                break; // To process messages
                            }

                        } while (State == BusState.Active);
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (!_handlers.Any()) // TODO REVIEW
                        {
                            Thread.CurrentThread.Interrupt();
                            return;
                        }

                        throw;
                    }
                }

                foreach (var msg in messages)
                {
                    try
                    {
                        handler(msg);
                    }
                    catch (ThreadInterruptedException)
                    {
                        //Thread.CurrentThread.Interrupt();
                        throw;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            while (true);
        }

        // O método PublishEvent​, que nunca bloqueia a thread invocante, envia a mensagem especificada para o bus de modo a que esta seja processada por todos
        // os handlers registados até ao momento, independentemente de estarem, ou não, a processar outra mensagem.
        // Caso existam mais do que maxPending mensagens para serem processadas pelo mesmo handler, o método PublishEvent deve descartar o evento para esse handler.
        // Após a chamada ao método Shutdown​, posteriores chamadas ao método PublishEvent devem lançar InvalidOperationException

        public void PublishEvent<E>(E message) where E : class
        {
            lock (_eventBusLock)
            {
                if (State != BusState.Active)
                {
                    throw new InvalidOperationException();
                }

                if (_handlers.ContainsKey(typeof(E)))
                {
                    var handler = _handlers[typeof(E)];

                    if (handler.BusMessages.Count < _maxPending)
                    {
                        handler.BusMessages.Add(message);

                        Monitor.PulseAll(_eventBusLock); // not efficient calling all handlers and proper handler could be in use, what to do? CyclicBarrier?
                    }
                    else
                    {
                        return; // discard message
                    }
                }
                else
                {
                    // no available handlers to process message
                }
            }
        }

        // // O método Shutdown deve bloquear a thread invocante até que o processo de shutdown esteja
        // concluído, isto é, tenha sido completado o processamento de todos as mensagens aceites pelo bus.

        public void Shutdown()
        {
            lock (_eventBusLock)
            {
                bool interrupted = false;

                if (State != BusState.Active)
                {
                    throw new InvalidOperationException(); // TODO REVIEW
                }

                State = BusState.InShutDown;

                Monitor.PulseAll(_eventBusLock);

                do
                {
                    try
                    {
                        Monitor.Wait(_eventBusLock);
                    }
                    catch (ThreadInterruptedException)
                    {
                        interrupted = true;
                    }
                   
                }
                while (State != BusState.Terminated);

                if (interrupted)
                    Thread.CurrentThread.Interrupt();
            }
        }
    }
}
