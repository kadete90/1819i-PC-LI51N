using System;
using System.Collections.Generic;
using System.Threading;
using Utils;

namespace Ex02
{
	// 2018-09-27: TODO implementation  with execution delegation
	// e.g.
	// private static class Request {
        // final int n;
        // boolean granted;
        // Request(int n) { this.n = n;}
    // }
	// private LinkedList<Request> waiters;
	
    public class AutoResetEvt
    {
        private bool _isEventSignaled;

        private readonly LinkedList<Thread> queue = new LinkedList<Thread>();

        //A chamada ao método await retorna de imediato se o evento estiver sinalizado fazendo reset ao estado de sinalização ou bloqueia a thread invocante até que: 
            //(a) ocorra uma notificação por invocação do método signal ou do método pulseAll; 
            //(b) expire o limite de tempo de espera especificado, ou; o bloqueio da thread seja interrompido.
        public bool await(int timeout) /*throw ThreadInterruptedException*/
        {
            lock (this)
            {
                if (_isEventSignaled)
                {
                    _isEventSignaled = false;
                    return true;
                }

				if (timeout == 0) return false;
				TimeoutHolder th = new TimeoutHolder(timeout);

				 try
				{
					do
					{
						var currentThread = Thread.CurrentThread;

						queue.AddLast(currentThread);

						Monitor.Wait(this, timeout);

						if (currentThread == queue.First.Value && _isEventSignaled)
						{
							_isEventSignaled = false;
							return true;
						}
						

						if (th.timeout() ) {
						{
							queue.Remove(currentThread);
							return false;
						}

					} while (true);
					
				}
                    catch (ThreadInterruptedException)
                    {
                        queue.Remove(currentThread);
                        throw;
                    }
            }
        }

        //coloca o estado do evento como sinalizado no caso de não existir nenhuma thread em espera ou liberta a thread há mais tempo em espera (FIFO). 
        public void signal()
        {
            lock (this)
            {
                if (queue.Count == 0)
                {
                    _isEventSignaled = true;
                    return;
                }

                //Thread threadWaitingLonger = queue.First.Value;

                queue.RemoveFirst();

                //threadWaitingLonger.Start();
            }
        }

        //só tem efeito se o estado do evento for não sinalizado e existirem threads em espera.
        //Nesse caso acorda todas as threads em espera voltando de imediato ao estado não sinalizado.
        public void pulseAll()
        {
            lock (this)
            {
                if (!_isEventSignaled && queue.Count > 0)
                {
                    Monitor.PulseAll(this);
                    _isEventSignaled = false;
                }
            }
        }
    }
}
