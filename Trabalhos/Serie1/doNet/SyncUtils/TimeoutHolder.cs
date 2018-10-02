using System;

namespace SyncUtils
{
    public class TimeoutHolder
    {

        private int expired;
        public static readonly long INFINITE = -1;

        public TimeoutHolder(int millis)
        {
            if (millis == INFINITE) expired = 0;
            else expired = Environment.TickCount + millis;
        }

        public bool isTimed()
        {
            return expired != 0;
        }

        public int Value
        {
            get
            {
                if (!isTimed()) return int.MaxValue;
                return Math.Max(0, expired - Environment.TickCount);
            }

        }

        public bool Timeout => Value == 0;
    }
}
