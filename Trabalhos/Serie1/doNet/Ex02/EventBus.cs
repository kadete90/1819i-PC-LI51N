using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ex02
{
    // Esta classe visa disponibilizar um mecanismo para que eventos dum sistema (e.g., utilizador registrou-se com sucesso, ligação à base de dados falhou)
    // sejam publicados para todos os subscritores interessados nesse tipo de evento.

    // Por exemplo o sistema pode ter um componente subscrito no evento de utilizador registrado com sucesso de forma a enviar-lhe um email de boas vindas.
    // A publicação dum evento consiste no envio de um objecto, designado por mensagem, para todos os subscritores registados no tipo desse objecto.

    public class EventBus : IDisposable
    {
        public class EventTypeHandler
        {
            public LinkedList<List<subscriber, object>> BusMessage;

            public EventTypeHandler(Type type)
            {
                BusMessage = new LinkedList<object>();
                Type = type;
                CurrentlyUsing = 0;
            }

            public EventTypeHandler(Type type, object message)
            {
                BusMessage = new LinkedList<object>();
                BusMessage.AddLast(message);
                Type = type;
                CurrentlyUsing = 0;
            }

            public Type Type { get; }

            public int CurrentlyUsing { get; set; }

            public int Waiters { get; set; }
        }

        private object _eventBusLock;
        private readonly int _maxPending;
        private Dictionary<Type, EventTypeHandler> _handlers = new Dictionary<Type, EventTypeHandler>();

        private volatile int publishing;
        private volatile bool shutdown;
        private Mutex shutdownMutex;
        
        public EventBus(int maxPending)
        {
            _eventBusLock = new object();
            _maxPending = maxPending;

            publishing = 0;
            shutdown = false;
            shutdownMutex = new Mutex(false);
        }

        // O método SubscribeEvent regista um handler para ser executado sempre que for publicada uma mensagem do tipo T​,
        // sendo o handler executado pela thread que procedeu ao respectivo registo com SubscribeEvent​.

        // Note-se que a chamada a este método é bloqueante, só retornando após um shutdown ou se a respectiva thread for interrompida.

        // O método SubscribeEvent deve lançar ThreadInterruptedException no caso da thread invocante ser interrompida enquanto estiver bloqueada ou durante a execução do handler.

        // Após a chamada ao método Shutdown​, todas as chamadas ao método SubscribeEvent deverão retornar após serem processadas todas as mensagens publicadas.

        public void SubscribeEvent<T>(Action<T> handler) where T : class
        {
            Monitor.Enter(_eventBusLock);

            if (shutdown)
            {
                return;
            }

            List<T> messagesTo

            if (!_handlers.ContainsKey(typeof(T)))
            {
                _handlers.Add(typeof(T), new EventTypeHandler(typeof(T)));
            }

            EventTypeHandler eventHandler;

            do
            {
                Monitor.Wait(_eventBusLock);

                if(_handlers.TryGetValue(typeof(T), out eventHandler))
                {
                    var aux = eventHandler.BusMessage;

                    eventHandler.BusMessage.Clear();

                    //cada subscritor tem a sua fila

                    while (eventHandler.BusMessage.Any())
                    {
                        eventHandler.BusMessage.First();
                    }

                    Monitor.Exit(_eventBusLock);

        
                    action.(message);

                    Monitor.Enter(_eventBusLock);
                }

                if (shutdown)
                {
                    return;
                }
            }
            while (true);

            Monitor.Exit(_eventBusLock);
        }

        // O método PublishEvent​, que nunca bloqueia a thread invocante, envia a mensagem especificada para o bus de modo a que esta seja processada por todos
        // os handlers registados até ao momento, independentemente de estarem, ou não, a processar outra mensagem.
        // Caso existam mais do que maxPending mensagens para serem processadas pelo mesmo handler, o método PublishEvent deve descartar o evento para esse handler.

        // Após a chamada ao método Shutdown​, posteriores chamadas ao método PublishEvent devem lançar InvalidOperationException

        public void PublishEvent<E>(E message) where E : class
        {
            lock (_eventBusLock)
            {
                if (shutdown)
                {
                    throw new InvalidOperationException();
                }

                if (_handlers.ContainsKey(typeof(E)))
                {
                    var handler = _handlers[typeof(E)];
                    if (handler.CurrentlyUsing < _maxPending)
                    {
                        handler.BusMessage.AddLast(message);

                        Monitor.PulseAll(_eventBusLock);
                    }
                }
                else
                {
                    _handlers.Add(typeof(E), new EventTypeHandler(typeof(E), message));
                }
            }
        }

        // // O método Shutdown deve bloquear a thread invocante até que o processo de shutdown esteja
        // concluído, isto é, tenha sido completado o processamento de todos as mensagens aceites pelo bus.

        public void Shutdown()
        {
            if (shutdown)
            {
                throw new InvalidOperationException(); // TODO REVIEW
            }

            lock (_eventBusLock)
            {
                shutdown = true;

                Monitor.PulseAll(_eventBusLock); // release all SubscribeEvent's

                if (publishing > 0)
                {
                    shutdownMutex.WaitOne();
                }
            }
        }

        public void Dispose()
        {
            shutdownMutex.Dispose();
        }
    }
}
