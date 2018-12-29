using System.Collections.Generic;

namespace Ex03
{
    //o sincronizador advertising panel que suporta a afixação de mensagens publicitárias pelas threads editoras, 
    //que ficarão expostas para consumo, por parte das threads consumidoras, durante o intervalo de tempo especificado.
    //A operação Publish publica uma mensagem publicitária definindo os respectivos conteúdo e tempo de exposição(em milissegundos). 
    //O painel pode ter apenas uma mensagem afixada de cada vez, pelo que a publicação de uma mensagem pode substituir a mensagem exposta anteriormente.
    //Sempre que é publicada uma mensagem, esta tem que ser obrigatoriamente entregue a todas as threads que se encontrem bloqueadas, 
    //mesmo quando o tempo de exposição da mensagem for zero(mensagens transitórias). 
    //As threads que pretendam consumir mensagens publicitárias invocam o método Consume, cuja execução poderá terminar: 
        //(1) devolvendo a instância do tipo M que contém uma mensagem publicitária válida;
        //(2) devolvendo null, se expirar o intervalo de tempo especificado pelo argumento timeout, ou;
        //(3) lançando ThreadInterruptedException, se a espera da thread for interrompida.

    public class AdvertisingPanel<M> where M : class
    {
        private readonly LinkedList<M> _queue;

        public AdvertisingPanel()
        {
            _queue = new LinkedList<M>();
        }

        public void Publish(M message, int exposureTime)
        {

        }

        public M Consume(int timeout) // throws ThreadInterruptedException
        {

        } 
    }

}
