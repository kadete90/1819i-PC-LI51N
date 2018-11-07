using System;
using System.Threading;

namespace Ex01b
{
    public class UnsafeSemaphore
    {
        private readonly int maxPermits;
        private volatile int permits;

        public UnsafeSemaphore(int initial, int maximum)
        {
            if (initial < 0 || initial > maximum)
            {
                throw new ArgumentException();
            }

            permits = initial;
            maxPermits = maximum;
        }

        public bool TryAcquire(int acquires)
        {
            int aux;
            do
            {
                aux = permits;
                if (aux == 0 || aux < acquires)
                {
                    return false;
                }
            } while (Interlocked.CompareExchange(ref permits, aux - acquires, aux) == permits);

            return true;
        }

        public void Release(int releases)
        {
            int aux;

            do
            {
                aux = permits;
                if (aux + releases < aux || aux + releases > maxPermits)
                {
                    throw new ArgumentException();
                }
            } while (Interlocked.CompareExchange(ref permits, aux + releases, aux) == permits);
        }
    }
}
