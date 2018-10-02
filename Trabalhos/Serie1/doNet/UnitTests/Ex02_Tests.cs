using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Ex02Tests
    {
        // Esta classe visa disponibilizar um mecanismo para que eventos dum sistema(e.g., utilizador registrou-se com sucesso, ligação à base de dados falhou)
        // sejam publicados para todos os subscritores interessados nesse tipo de evento.

        // Por exemplo o sistema pode ter um componente subscrito no evento de utilizador registrado com sucesso de forma a enviar-lhe um email de boas vindas.
        // A publicação dum evento consiste no envio de um objecto, designado por mensagem, para todos os subscritores registados no tipo desse objecto.

        // Tipos diferentes de eventos são representados por tipos .NET diferentes.

        // O método SubscribeEvent regista um handler para ser executado sempre que for publicada uma mensagem do tipo T​,
        // sendo o handler executado pela thread que procedeu ao respectivo registo com SubscribeEvent​.

        // Note-se que a chamada a este método é bloqueante, só retornando após um shutdown ou se a respectiva thread for interrompida.

        // O método PublishEvent​, que nunca bloqueia a thread invocante, envia a mensagem especificada 
        // para o bus de modo a que esta seja processada por todos os handlers registados até ao momento, independentemente de estarem, ou não, a processar outra mensagem.
        // Caso existam mais do que maxPending mensagens para serem processadas pelo mesmo handler, o método PublishEvent deve descartar o evento para esse handler.

        // O método SubscribeEvent deve lançar ThreadInterruptedException no caso da thread invocante ser interrompida enquanto estiver bloqueada ou durante a execução do handler.

        // Após a chamada ao método Shutdown​, posteriores chamadas ao método PublishEvent devem lançar InvalidOperationException e todas
        // as chamadas ao método SubscribeEvent deverão retornar após serem processadas todas as mensagens
        // publicadas. O método Shutdown deve bloquear a thread invocante até que o processo de shutdown esteja
        // concluído, isto é, tenha sido completado o processamento de todos as mensagens aceites pelo bus.

        [Test]
        public void TestMethod1()
        {
        }
    }
}
