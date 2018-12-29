namespace Ex05
{
    //Implemente em C# o sincronizador exchanger Exchanger<T> que permite a troca, entre pares de threads,
    //de mensagens definidas por instâncias do tipo T. A classe que implementa o sincronizador deve definir,
    //pelo menos, o método bool Exchange(T mine, int timeout, out T yours),
    //que é chamado pelas threads para oferecer uma mensagem (parâmetro mine)
    //e receber a mensagem oferecida pela thread com que emparelham (parâmetro yours).
    //Quando a troca de mensagens não pode ser realizada de imediato (não existe já uma thread bloqueada),
    //a thread corrente fica bloqueada até que outra thread invoque o método Exchange,
    //seja interrompida ou expire o limite de tempo, especificado através do parâmetro timeout.

    public class Exchanger<T>
    {
        public bool Exchange(T mine, int timeout, out T yours)
        {

        }

    }
}
