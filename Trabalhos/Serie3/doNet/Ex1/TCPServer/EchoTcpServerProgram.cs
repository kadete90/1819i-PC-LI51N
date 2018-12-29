using System;
using System.Threading;
using System.Threading.Tasks;
using TCPServer.Logging;

namespace TCPServer
{
    public class EchoTcpServerProgram
    {
        private const int port = 8081;
        private static int counter;

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly Terminator terminator = new Terminator();

        public static async Task Run()
        {
            AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
            Console.CancelKeyPress += HandleCancel;

            Log.Info("starting server");
            EchoTcpServer server = new EchoTcpServer();
            using (terminator.Enter())
            {
                await server.Start(port, cts.Token);
                Log.Info("ending, bye");
            }
        }

        static void Main(string[] args)
        {
            Run();
        }

        //public static async void Run()
        //{
        //    var listener = new TcpListener(IPAddress.Loopback, port);
        //    listener.Start();
        //    Log.Info($"Listening on {port}");
        //    while (true)
        //    {
        //        var client = await listener.AcceptTcpClientAsync();
        //        var id = counter++;
        //        Log.Info($"connection accepted with id '{id}'");
        //        Handle(id, client);
        //    }
        //}

        private static readonly ILog Log = LogProvider.For<EchoTcpServerProgram>();

        private static void HandleCancel(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cts.Cancel();
            // The application will endup normally on the Main flow
        }

        private static void HandleProcessExit(object sender, EventArgs eventArgs)
        {
            Log.Info($"handling process exit");
            cts.Cancel();
            terminator.Shutdown().Wait();
        }
    }
}
