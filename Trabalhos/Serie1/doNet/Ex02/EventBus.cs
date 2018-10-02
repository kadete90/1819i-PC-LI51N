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
        public class EventHandler<T> where T : class
        {
            public EventHandler(Action<T> action)
            {
                Action = action;
                CurrentlyUsing = 0;
            }

            public Action<T> Action { get; set; }
            public int CurrentlyUsing { get; set; }
        }

        private object _eventBusLock;
        private readonly int _maxPending;
        private List<EventHandler<Type>> _events = new List<EventHandler<Type>>();

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
            lock (_eventBusLock)
            {
                if (shutdown)
                {
                    return;
                }

                _events.Add(new EventHandler<Type>(handler as Action<Type>));

                do
                {
                    Monitor.Wait(_eventBusLock);

                    if (shutdown)
                    {
                        return;
                    }
                }
                while (true);
            }
        }

        // O método PublishEvent​, que nunca bloqueia a thread invocante, envia a mensagem especificada para o bus de modo a que esta seja processada por todos
        // os handlers registados até ao momento, independentemente de estarem, ou não, a processar outra mensagem.
        // Caso existam mais do que maxPending mensagens para serem processadas pelo mesmo handler, o método PublishEvent deve descartar o evento para esse handler.

        // Após a chamada ao método Shutdown​, posteriores chamadas ao método PublishEvent devem lançar InvalidOperationException

        public void PublishEvent<E>(E message) where E : class
        {
            if (shutdown)
            {
                throw new InvalidOperationException();
            }

            Interlocked.Increment(ref publishing);

            try
            {
                foreach (EventHandler<E> evHandler in _events.OfType<EventHandler<E>>())
                {
                    var evHandlerCurrentlyUsing = evHandler.CurrentlyUsing;

                    if (evHandlerCurrentlyUsing < _maxPending) // TODO REVIEW comparison in a non blocking code
                    {
                        Interlocked.Increment(ref evHandlerCurrentlyUsing);

                        evHandler.Action(message);

                        Interlocked.Decrement(ref evHandlerCurrentlyUsing);
                    }
                    else
                    {
                        // descartar
                    }
                }
            }
            finally // called both on success or exception
            {
                lock (_eventBusLock)
                {
                    Interlocked.Decrement(ref publishing);

                    if (shutdown && publishing == 0)
                    {
                        shutdownMutex.ReleaseMutex();
                    }
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
