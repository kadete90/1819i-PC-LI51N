using System;
using System.Threading;
using System.Threading.Tasks;
using Ex02;
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

        private static void Print(string s)
        {
            Console.WriteLine(s);
        }

        class MyLogError
        {
            public string Message { get; set; }
            public int ErrorCode { get; set; }
        }

        private static void PrintError(MyLogError s)
        {
            Console.WriteLine(s.ErrorCode + ": " + s.Message);
        }

        [Test]
        public void Test_01_Subscribe_and_Publish_Events()
        {
            EventBus bus = new EventBus(5);

            new Thread(() => bus.SubscribeEvent<string>(Print)).Start();
            new Thread(() => bus.SubscribeEvent<MyLogError>(PrintError)).Start();

            Thread.Sleep(1000);

            int counter = 0;

            for (int i = 0; i < 100; i++)
            {
                var aux = i;
                Task.Run(() =>
                {
                    Interlocked.Increment(ref counter);
                    bus.PublishEvent($"{aux} event occurred");
                });
            }

            bus.PublishEvent(new MyLogError
            {
                ErrorCode = 20,
                Message = @"Ligação à base de dados falhou"
            });

            Thread.Sleep(1000);

            bus.Shutdown();

            Assert.IsTrue(counter == 100);
        }

        [Test]
        public void Test_02_ShutDown_Exception()
        {
            EventBus bus = new EventBus(5);

            new Thread(() => bus.SubscribeEvent<MyLogError>(PrintError)).Start();

            bus.Shutdown();

            try
            {
                bus.PublishEvent(new MyLogError
                {
                    ErrorCode = 20,
                    Message = @"Ligação à base de dados falhou"
                });

                Assert.IsTrue(false);
            }
            catch (InvalidOperationException e)
            {
                Assert.IsTrue(true);
            }
        }
    }
}
