using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Producer
{
    //static private bool pipeStateValid = false;
    static private bool IstartedClient = false;

    static async Task Main()
    {
        using var pipe = new NamedPipeServerStream("MyPipe", PipeDirection.InOut, 1, 
            PipeTransmissionMode.Message);

        Console.WriteLine("Producer: Waiting for Consumer to connect...");

        //var allProcesses = Process.GetProcesses().Select(p => p.ProcessName).ToList();
        //allProcesses.Sort();

        var consumerProcess = Process.GetProcesses()
            .Where(p => p.ProcessName == "LearnIPC6_Consumer").FirstOrDefault();
        if(consumerProcess == null)
        {
            var consumerExePath = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "../../../..", "LearnIPC6_Consumer",
                    "bin", "Debug", "net6.0", "LearnIPC6_Consumer.exe"));
            if (File.Exists(consumerExePath))
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = consumerExePath,
                    UseShellExecute = true, // Set to true to open the process in a new console window
                    CreateNoWindow = false,  // Set to false to show the console window
                };

                //consumerProcess = Process.Start(consumerExePath);
                using Process consumerProcess_ = new();
                consumerProcess_.StartInfo = startInfo;
                consumerProcess_.Start();
                IstartedClient = true;
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    try
                    {
                        consumerProcess.Kill();
                    }
                    catch (Exception) { }
                };
            }

        }

        await pipe.WaitForConnectionAsync();
        Console.WriteLine("Producer: Consumer has connected!");

        var cts = new CancellationTokenSource();

        // Handle incoming messages from Consumer asynchronously
        _ = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            while (!cts.Token.IsCancellationRequested)
            {
                int bytesRead = await pipe.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\n              Received from Consumer: {message}");
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
            theMessage = $"{randomMessage.NextDouble() * 100}"; ;
            byte[] messageBytes = Encoding.UTF8.GetBytes(theMessage);
            try
            {
                Console.WriteLine($"Producer sending {theMessage}.     ");
                await pipe.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch(IOException e)
            {
                int i = 8;
            }
            //int sleepTime = Convert.ToInt32(randomSleepTime.NextDouble() * 5.0);
            //Thread.Sleep(sleepTime);
        }
        cts.Cancel();
    }
}
