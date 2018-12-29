using System;
using System.Threading;

namespace Ex04
{
    public class UnsafeSpinLazy<T> where T : class
    {
        private const int UNCREATED = 0, BEING_CREATED = 1, CREATED = 2;
        private int state = UNCREATED;
        private Func<T> factory;
        private T value;

        public UnsafeSpinLazy(Func<T> factory)
        {
            this.factory = factory;
        }

        public bool IsValueCreated
        {
            get
            {
                return state == CREATED;
            }
        }

        public T Value
        {
            get
            {
                SpinWait sw = new SpinWait();

                do
                {
                    if (state == CREATED)
                    {
                        break;
                    }
                    else if (state == UNCREATED)
                    {
                        state = BEING_CREATED;
                        value = factory();
                        state = CREATED;
                        break;
                    }

                    sw.SpinOnce();

                } while (true);

                return value;
            }
        }
    }
}
