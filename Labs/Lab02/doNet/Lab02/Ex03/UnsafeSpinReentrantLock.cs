using System.Threading;

namespace Ex03
{
    public class UnsafeSpinReentrantLock
    {
        private class ThreadBoxed
        {
            public volatile int remaining;

            internal ThreadBoxed(int value)
            {
                this.remaining = value;
            }
        }

        private Thread owner;
        private int count;

        public bool TryLock()
        {
            if (owner == Thread.CurrentThread)
            {
                count++;
                return true;
            }

            if (owner == null)
            {
                owner = Thread.CurrentThread;
                return true;
            }

            return false;
        }
        public void Lock()
        {
            while (!TryLock())
            {
                Thread.Yield();
            }
        }
        public void Unlock()
        {
            if (owner != Thread.CurrentThread)
            {
                throw new ThreadStateException();
            }

            if (count == 0)
            {
                owner = null;
            }
            else
            {
                --count;
            }
        }
    }
}
