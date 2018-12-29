using System;
using System.Threading;
using Utils;

namespace Lab01
{
    // Usando monitores Java ou CLI, implemente o sincronizador future holder usado para hospedar dados resultantes de cálculos demorados.
    // A operação getValue bloqueia as threads invocantes até que os dados sejam disponibilizados através da chamada a setValue.
    // Como as instâncias desta classe são de utilização única, chamadas subsequentes a setValue produzem excepção (IllegalStateException). 
    // A operação getValue retorna os dados hospedados, ou null caso ocorra timeout.
    // O sincronizador suporta cancelamento das threads em espera.

    public class FutureHolder<T> where T : class
    {
        private object _lock { get; set; }
        private T _value { get; set; }

        private bool _isValueAvailable;

        public FutureHolder()
        {
            _lock = new object();
            _isValueAvailable = false;
        }

        public void SetValue(T value)
        {
            lock (_lock)
            {
                if (_isValueAvailable)
                {
                    throw new InvalidOperationException();
                }

                _value = value;
                _isValueAvailable = true;
            }

            Monitor.PulseAll(_lock);
        }

        public T GetValue(int timeout)
        {
            lock (_lock)
            {
                if (_isValueAvailable)
                {
                    return _value;
                }

                int lastTime = timeout != Timeout.Infinite ? Environment.TickCount : 0;

                do
                {
                    Monitor.Wait(_lock, timeout);

                    if (_isValueAvailable)
                    {
                        return _value;
                    }

                    if (SyncUtils.AdjustTimeout(ref lastTime, ref timeout) == 0)
                    {
                        return null;
                    }
                }
                while (true);
            }
        }
    }
}