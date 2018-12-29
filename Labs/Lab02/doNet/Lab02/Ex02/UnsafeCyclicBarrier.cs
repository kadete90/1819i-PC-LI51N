using System;
using System.Threading;

namespace Ex02b
{
    public class UnsafeCyclicBarrier
    {
        private class PhaseBoxed
        {
            public volatile int remaining;

            internal PhaseBoxed(int value)
            {
                this.remaining = value;
            }
        }

        private readonly int partners;
        private volatile PhaseBoxed currentPhase;

        public UnsafeCyclicBarrier(int partners)
        {
            if (partners <= 0)
            {
                throw new ArgumentException();
            }

            currentPhase = new PhaseBoxed(partners);
            this.partners = partners;
        }

        public void SignalAndAwait()
        {
            PhaseBoxed phase = currentPhase;

            do
            {
                var obsRemaining = phase.remaining;
                
                if (obsRemaining == 0)
                {
                    throw new InvalidOperationException();
                }

                if (Interlocked.CompareExchange(ref phase.remaining, obsRemaining - 1, obsRemaining) == obsRemaining)
                {
                    if (obsRemaining == 1)
                    {
                        currentPhase = new PhaseBoxed(partners);
                    }
                    else
                    {
                        while (phase == currentPhase)
                        {
                            Thread.Yield();
                        }
                    }
                
                    return;
                }
              
            } while (true);
        }
    }
}