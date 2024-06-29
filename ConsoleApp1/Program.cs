// See https://aka.ms/new-console-template for more information
using ConsoleApp1;

Console.WriteLine("Hello, World!");
var server = new ModbusTcpServer("127.0.0.1", 502);
server.Start();