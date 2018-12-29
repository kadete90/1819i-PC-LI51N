using System;

namespace Ex01b
{
    public class UnsafeSemaphoreOriginal
    {
        private int maxPermits, permits;
        public UnsafeSemaphoreOriginal(int initial, int maximum)
        {
            if (initial < 0 || initial > maximum)
            {
                throw new ArgumentException();
            }

            permits = initial; maxPermits = maximum;
        }
        public bool TryAcquire(int acquires)
        {
            if (permits < acquires)
            {
                return false;
            }

            permits -= acquires;
            return true;
        }
        public void Release(int releases)
        {
            if (permits + releases < permits || permits + releases > maxPermits)
            {
                throw new ArgumentException();
            };
            permits += releases;
        }
    }
}
