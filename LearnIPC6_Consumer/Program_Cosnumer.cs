using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Consumer
{
    static async Task Main()
    {
        using var pipe = new NamedPipeClientStream(".", "MyPipe", PipeDirection.InOut, PipeOptions.Asynchronous);

        Console.WriteLine("Consumer: Connecting to Producer...");
        while (!pipe.IsConnected)
        {
            try
            {
                await pipe.ConnectAsync(1000);
            }
            catch (TimeoutException)
            {
                // Pipe not yet available, retry in a moment
            }
        }
        Console.WriteLine("Connected to Producer!");

        var cts = new CancellationTokenSource();

        // Handle incoming messages from Producer asynchronously
        _ = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            while (!cts.Token.IsCancellationRequested)
            {
                int bytesRead = await pipe.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\n               Received from Producer: {message}");
                }
            }
        }, cts.Token);


        Random randomMessage = new Random();
        Random randomSleepTime = new Random();
        int maxMessages = 10;
        int messageCount = 0;
        // Send messages to Consumer
        while (messageCount++ < maxMessages)
        {
            string theMessage = string.Empty;
            theMessage = $"{randomMessage.NextDouble() * -100}"; ;
            byte[] messageBytes = Encoding.UTF8.GetBytes(theMessage);
            try
            {
                Console.WriteLine($"Consumer Sending {theMessage}.     ");
                await pipe.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch (IOException e)
            {
                int i = 8;
            }
            //int sleepTime = Convert.ToInt32(randomSleepTime.NextDouble() * 5.0);
            //Thread.Sleep(sleepTime);
        }
        cts.Cancel();


    }
}
