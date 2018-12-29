using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCPServer.Logging;
using Utils;

namespace TCPServer
{    
    class EchoTcpServer
    {
        private static readonly ILog Log = LogProvider.For<EchoTcpServer>();

        private static readonly JsonSerializer serializer = new JsonSerializer();

        private readonly Terminator terminator = new Terminator();

        static volatile Dictionary<string, List<Request>> queues = new Dictionary<string, List<Request>>();

        public async Task Start(int port, CancellationToken ctoken)
        {
            var connectionId = 0;
            var ipAddress = IPAddress.Loopback;//Dns.GetHostEntry("localhost").AddressList[0];
            var listener = new TcpListener(ipAddress, port);

            // needed because there isn't another way to cancel the
            // AcceptTcpClientAsync
            using (ctoken.Register(() =>
            {
                Log.Info("stopping listener");
                listener.Stop();
            }))
            {
                try
                {
                    listener.Start();
                    Log.Info($"listening on {port}");
                    for (; !ctoken.IsCancellationRequested;)
                    {
                        Log.Info("accepting...");
                        var client = await listener.AcceptTcpClientAsync();
                        Log.Info("...client accepted");
                        connectionId += 1;
                        Echo(client, ctoken, connectionId);
                    }
                }
                catch (Exception e)
                {
                    // await AcceptTcpClientAsync will end up with an exception
                    Log.Info($"Exception '{e.Message}' received");
                }
            }

            await terminator.Shutdown();
        }

        private static async void Handle(int id, TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var reader = new JsonTextReader(new StreamReader(stream))
                {
                    // To support reading multiple top-level objects
                    SupportMultipleContent = true
                };
                var writer = new JsonTextWriter(new StreamWriter(stream));
                while (true)
                {
                    try
                    {
                        // to consume any bytes until start of object ('{')
                        do
                        {
                            await reader.ReadAsync();
                            Console.WriteLine($"advanced to {reader.TokenType}");
                        } while (reader.TokenType != JsonToken.StartObject && reader.TokenType != JsonToken.None);

                        if (reader.TokenType == JsonToken.None)
                        {
                            Console.WriteLine($"[{id}] reached end of input stream, ending.");
                            return;
                        }

                        var json = await JObject.LoadAsync(reader);
                        // to ensure that proper deserialization is possible
                        Request request = json.ToObject<Request>();

                        var response = new Response
                        {
                            Payload = json
                        };

                        switch (request.Method)
                        {

                            case "CREATE":
                                {
                                    //CREATE - garante a existência da fila com o nome definido no campo path;
                                    break;
                                }
                            case "SEND":
                                {
                                    //SEND - envia a mensagem presente em payload para a fila com nome definido em path;
                                    if (!queues.ContainsKey(json.Path))
                                    {
                                        response.Status = Status.MissingQueue;
                                    }
                                    else
                                    {

                                    }

                                    break;
                                }
                            case "RECEIVE":
                                {
                                    //RECEIVE - retira uma mensagem da fila com nome definido em path e retorna-a no campo payload.
                                    //O tempo máximo de espera, em milisegundos, é definido no header timeout.
                                    break;
                                }
                            case "SHUTDOWN":
                                {
                                    //inicia o processo de encerramento do servidor, ficando à espera que este esteja concluído.
                                    //O tempo máximo de espera, em milisegundos, é definido no header timeout.

                                    await terminator.Shutdown();

                                    break;
                                }
                            default:
                                {
                                    response.Status = Status.ServerError;

                                    serializer.Serialize(writer, response);
                                    await writer.FlushAsync();
                                    return;
                                }
                        }

                        response.Status = Status.Success;

                        serializer.Serialize(writer, response);
                        await writer.FlushAsync();
                    }
                    catch (JsonReaderException e)
                    {
                        Console.WriteLine($"[{id}] Error reading JSON: {e.Message}, continuing");

                        var response = new Response
                        {
                            Status = Status.ServerError,
                        };

                        serializer.Serialize(writer, response);
                        await writer.FlushAsync();
                        // close the connection because an error may not be recoverable by the reader
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{id}] Unexpected exception, closing connection {e.Message}");
                        return;
                    }
                }
            }
        }

        private async void Echo(TcpClient client, CancellationToken ctoken, int connectionId)
        {
            const int cap = 1024;
            var buffer = new byte[cap];
            var stream = client.GetStream();
            var tcs = new TaskCompletionSource<int>();
            using (terminator.Enter())
            using (client)
            using (stream)
            using (ctoken.Register(() => tcs.SetCanceled()))
            {
                for (;;)
                {
                    try
                    {
                        var readBytes = await ReadFromStreamAsync(stream, buffer, ctoken, tcs.Task);
                        Log.Info($"[{connectionId}] read '{readBytes}' bytes");
                        if (readBytes == 0)
                        {
                            Log.Info($"[{connectionId}] ending.");
                            return;
                        }

                        await stream.WriteAsync(buffer, 0, readBytes, ctoken);
                    }
                    catch (TaskCanceledException e)
                    {
                        // This looses data if the read or write can cancelled
                        client.Close();
                        Log.Info($"[{connectionId}] cancelled: '{e}'");
                        return;
                    }
                }
            }
        }

        // Because the underlying socket implementation does not support cancellation
        private Task<int> ReadFromStreamAsync(
            NetworkStream stream,
            byte[] buffer,
            CancellationToken ctoken,
            Task<int> cancellationTask)
        {
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length, ctoken);
            return Task.WhenAny(readTask, cancellationTask).Unwrap();
        }
    }

    // The example program
    //class EchoTcpServerProgram
    //{
    //    private static readonly ILog Log = LogProvider.For<EchoTcpServerProgram>(); 
        
    //    private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    //    private static readonly Terminator terminator = new Terminator();

    //    public static async Task Run()
    //    {
    //        AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
    //        Console.CancelKeyPress += HandleCancel;

    //        Log.Info("starting server");
    //        EchoTcpServer server = new EchoTcpServer();
    //        using (terminator.Enter())
    //        {
    //            await server.Start(8081, cts.Token);
    //            Log.Info("ending, bye");
    //        }
    //    }

    //    private static void HandleCancel(object sender, ConsoleCancelEventArgs e)
    //    {
    //        e.Cancel = true;
    //        cts.Cancel();
    //        // The application will endup normally on the Main flow
    //    }

    //    private static void HandleProcessExit(object sender, EventArgs eventArgs)
    //    {
    //        Log.Info($"handling process exit");
    //        cts.Cancel();
    //        terminator.Shutdown().Wait();
    //    }
    }
}
