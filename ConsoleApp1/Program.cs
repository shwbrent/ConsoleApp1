// See https://aka.ms/new-console-template for more information
using ConsoleApp1;

Console.WriteLine("Hello, World!");
var server = new ModbusTcpServer("192.168.8.139", 502);
// 更新輸入寄存器
server.UpdateInputRegister(0, 1234);
server.UpdateInputRegister(1, 5678);

// 更新線圈
server.UpdateCoil(0, true);
server.UpdateCoil(1, false);

// 更新離散輸入
server.UpdateDiscreteInput(0, true);
server.UpdateDiscreteInput(1, false);

// 更新保持寄存器
server.UpdateHoldingRegister(0, 4321);
server.UpdateHoldingRegister(1, 8765);
server.Start();
