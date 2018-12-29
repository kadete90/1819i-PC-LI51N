using System;
using System.Threading;

namespace Ex02b
{
    public class UnsafeCyclicBarrierOriginal
    {
        private readonly int partners;
        private int remaining, currentPhase;
        public UnsafeCyclicBarrierOriginal(int partners)
        {
            if (partners <= 0)
            {
                throw new ArgumentException();
            }

            this.partners = this.remaining = partners;
        }
        public void SignalAndAwait()
        {
            int phase = currentPhase;

            if (remaining == 0)
            {
                throw new InvalidOperationException();
            }

            if (--remaining == 0)
            {
                remaining = partners; currentPhase++;
            }
            else
            {
                while (phase == currentPhase)
                {
                    Thread.Yield();
                }
            }
        }
    }
}