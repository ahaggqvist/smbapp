using System.Net;
using Microsoft.Extensions.Hosting;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace smbapp;

public static class Program
{
    private const string IpAddress = "0.0.0.0";
    private const string Username = "CHANGEME";
    private const string Password = "CHANGEIT";
    private const string Domain = "CHANGEME";
    private const string ShareName = "Shared";

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var client = new SMB2Client();
                var isConnected = client.Connect(IPAddress.Parse(IpAddress), SMBTransportType.DirectTCPTransport);
                if (!isConnected)
                {
                    Console.WriteLine("Connection failed ...");
                    return;
                }

                var status = client.Login(Domain, Username, Password);
                Console.WriteLine(status);

                if (status == NTStatus.STATUS_SUCCESS)
                {
                    var smbFileStore = client.TreeConnect(ShareName, out status);
                    Console.WriteLine($"TreeConnect: {status}");

                    status = smbFileStore.CreateFile(out var directoryHandle, out _, string.Empty, AccessMask.GENERIC_READ, FileAttributes.Directory,
                        ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                    Console.WriteLine($"CreateHandle: {status}");

                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        status = smbFileStore.QueryDirectory(out List<QueryDirectoryFileInformation> fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                        Console.WriteLine($"QueryDirectory: {status}");

                        status = smbFileStore.CloseFile(directoryHandle);
                        Console.WriteLine($"CloseFile: {status}");

                        foreach (var file in fileList)
                        {
                            var fileInformation = (FileDirectoryInformation)file;
                            Console.WriteLine(fileInformation.FileName);
                        }

                        var fileStore = client.TreeConnect(ShareName, out status);
                        var filePath = $"{DateTime.Now:yyyyMMddHHmmssmm}.txt";
                        if (fileStore is SMB1FileStore)
                        {
                            filePath = @"\\" + filePath;
                        }

                        status = fileStore.CreateFile(out var fileHandle, out _, filePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal,
                            ShareAccess.None,
                            CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                        if (status == NTStatus.STATUS_SUCCESS)
                        {
                            var data = "Hello"u8.ToArray();
                            status = fileStore.WriteFile(out _, fileHandle, 0, data);
                            Console.WriteLine($"WriteFile: {status}");

                            if (status != NTStatus.STATUS_SUCCESS)
                            {
                                throw new Exception("Failed to write to file");
                            }

                            status = fileStore.CloseFile(fileHandle);
                            Console.WriteLine($"Closefile: {status}");
                        }
                    }

                    status = smbFileStore.Disconnect();
                    Console.WriteLine($"Disconnect: {status}");

                    client.Logoff();
                }

                client.Disconnect();
            });
    }
}