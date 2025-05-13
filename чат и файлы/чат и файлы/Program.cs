using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Выберите режим:");
        Console.WriteLine("1. UDP Чат");
        Console.WriteLine("2. Отправить файл (UDP Клиент)");
        Console.WriteLine("3. Принять файл (UDP Сервер)");
        Console.Write("Введите номер режима: ");

        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                Chat();
                break;
            case "2":
                FileSender();
                break;
            case "3":
                FileReceiver();
                break;
            default:
                Console.WriteLine("Неверный выбор.");
                break;
        }
    }

    // Чат 
    static void Chat()
    {
        Console.Write("Введите порт для прослушивания: ");
        int localPort = int.Parse(Console.ReadLine());

        Console.Write("Введите IP получателя: ");
        string remoteIp = Console.ReadLine();

        Console.Write("Введите порт получателя: ");
        int remotePort = int.Parse(Console.ReadLine());

        UdpClient udpClient = new UdpClient(localPort);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);

        // Поток для приема сообщений
        new Thread(() =>
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEP = null;
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"\n[Сообщение от {remoteEP}]: {message}");
                    Console.Write("> ");
                }
                catch { break; }
            }
        }).Start();

        Console.WriteLine("Введите сообщения. Для выхода напишите 'exit'.");
        while (true)
        {
            Console.Write("> ");
            string message = Console.ReadLine();
            if (message.ToLower() == "exit") break;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, remoteEndPoint);
        }

        udpClient.Close();
    }

    // Отправка
    static void FileSender()
    {
        const int serverPort = 9000;

        Console.Write("Введите IP сервера: ");
        string serverIp = Console.ReadLine();

        UdpClient client = new UdpClient();

        try
        {
            // Пытаемся отправить "ping" и ждем ответ
            string pingMessage = "PING";
            byte[] pingData = Encoding.UTF8.GetBytes(pingMessage);
            client.Send(pingData, pingData.Length, serverIp, serverPort);

            client.Client.ReceiveTimeout = 3000;
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] response = client.Receive(ref remoteEP);
            string responseText = Encoding.UTF8.GetString(response);

            if (responseText != "PONG")
            {
                Console.WriteLine("Сервер не ответил корректно.");
                return;
            }

            Console.WriteLine("Соединение с сервером установлено.");
        }
        catch
        {
            Console.WriteLine("Не удалось подключиться к серверу.");
            return;
        }

        Console.Write("Введите путь к файлу: ");
        string filePath = Console.ReadLine();

        while (!File.Exists(filePath))
        {
            Console.WriteLine("Файл не найден.");
            Console.WriteLine();
            Console.Write("Введите путь к файлу: ");
            filePath = Console.ReadLine();
        }

        string fileName = Path.GetFileName(filePath);
        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
        client.Send(nameBytes, nameBytes.Length, serverIp, serverPort);

        byte[] fileBytes = File.ReadAllBytes(filePath);
        client.Send(fileBytes, fileBytes.Length, serverIp, serverPort);

        Console.WriteLine("Файл отправлен.");
        client.Close();
    }

    // Получаение
    static void FileReceiver()
    {
        const int port = 9000;
        UdpClient server = new UdpClient(port);
        Console.WriteLine("Ожидание клиента...");

        IPEndPoint remoteEP = null;

        // Принимаем "PING"
        byte[] pingData = server.Receive(ref remoteEP);
        string pingMessage = Encoding.UTF8.GetString(pingData);

        if (pingMessage == "PING")
        {
            byte[] pong = Encoding.UTF8.GetBytes("PONG");
            server.Send(pong, pong.Length, remoteEP);
            Console.WriteLine($"Получен запрос от клиента: {remoteEP}");
        }
        else
        {
            Console.WriteLine("Ожидался PING, но получено другое сообщение.");
            server.Close();
            return;
        }

        // Получаем имя файла
        byte[] fileNameData = server.Receive(ref remoteEP);
        string fileName = Encoding.UTF8.GetString(fileNameData);
        Console.WriteLine($"Получено имя файла: {fileName}");

        // Спрашиваем путь для сохранения
        Console.Write("Введите путь для сохранения файла (папка): ");
        string saveDir = Console.ReadLine();
        while (!Directory.Exists(saveDir))
        {
            Console.WriteLine("Папка не существует. Файл не сохранён.");
            Console.WriteLine();
            Console.Write("Введите путь для сохранения файла (папка): ");
            saveDir = Console.ReadLine();
        }

        byte[] fileData = server.Receive(ref remoteEP);
        string savePath = Path.Combine(saveDir, fileName);
        File.WriteAllBytes(savePath, fileData);

        Console.WriteLine($"Файл успешно сохранён по пути: {savePath}");
        server.Close();
    }
}
