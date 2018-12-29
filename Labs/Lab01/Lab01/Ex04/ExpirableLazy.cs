using System;
using System.Threading;

namespace Ex04
{
    //Esta classe implementa uma versão da classe System.Lazy<T>​, pertencente à plataforma.NET, thread-safe e
    //com limitação no tempo de vida, especificado através do parâmetro timeToLive​, do valor calculado.
    //O acesso à propriedade Value deve ter o seguinte comportamento: 
        //(a) caso o valor já tenha sido calculado e o seu tempo de vida ainda não tenha expirado, retorna esse valor; 
        //(b) caso o valor ainda não tenha sido calculado ou o tempo de vida já tenha sido ultrapassado, inicia o cálculo chamando provider na própria thread invocante e retorna o valor resultante;  
        //(c) caso já existe outra thread a realizar esse cálculo, espera até que o valor esteja calculado; 
        //(d) lança ThreadInterruptedException se a espera da thread for interrompida. Caso a chamada a provider resulte numa excepção: 
            //(a) a chamada a Value nessa thread deve resultar no lançamento dessa excepção; 
            //(b) se existirem outras threads à espera do valor, deve ser seleccionada uma delas para a retentativa do cálculo através da função provider​.
                //Não existe limite no número de retentativas.  O tempo de vida inicia-se quando o valor é retornado da função provider

    public class ExpirableLazy<T> where T : class
    {
        private readonly Func<T> _provider;
        private readonly TimeSpan _liveUntilTime;

        private volatile bool _executing;

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive)
        {
            _provider = provider;
            _liveUntilTime = DateTime.Now.TimeOfDay + timeToLive;
        }

        private T _result;

        public T Value // throws InvalidOperationException, ThreadInterruptedException
        {
            get
            {
                lock (this)
                {
                    if (_result != null && DateTime.Now.TimeOfDay <= _liveUntilTime)
                    {
                        return _result;
                    }

                    if (!_executing)
                    {
                        _executing = true;
                    }
                    else
                    {
                        do
                        {
                            Monitor.Wait(this, _liveUntilTime);

                            if (_result != null && DateTime.Now.TimeOfDay <= _liveUntilTime)
                            {
                                return _result;
                            }
                            if (!_executing)
                            {
                                _executing = true;
                                break;
                            }

                        } while (true);
                    }
                }

                T aux = null;
                Exception ex = null;

                try
                {
                    aux = _provider();
                }
                catch (Exception e)
                {
                    ex = e;
                }

                lock (this)
                {
                    _executing = false;

                    if (ex != null)
                    {
                        throw new InvalidOperationException(ex.Message, ex);
                    }

                    _result = aux;

                    Monitor.PulseAll(this);
                    return _result;
                }
            }
        }
    }
}
