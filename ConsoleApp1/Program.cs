// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using System.IO.Ports;

Console.WriteLine("Program Start");
//var server = new ModbusTcpServer("192.168.8.139", 502, 30);
//// 更新輸入寄存器
//server.UpdateInputRegister(0, 1234);
//server.UpdateInputRegister(1, 5678);

//// 更新線圈
//server.UpdateCoil(0, true);
//server.UpdateCoil(1, false);

//// 更新離散輸入
//server.UpdateDiscreteInput(0, true);
//server.UpdateDiscreteInput(1, false);

//// 更新保持寄存器
//server.UpdateHoldingRegister(0, 4321);
//server.UpdateHoldingRegister(1, 8765);

//// float test
////float gauge = 1e-2f;
//float[] sec = new float[] { 5.334444e-22f };
//ushort[] uintData = new ushort[2];
//Buffer.BlockCopy(sec, 0, uintData, 0, 4);
//server.UpdateHoldingRegister(2, uintData[0]);
//server.UpdateHoldingRegister(3, uintData[1]);

//server.Start();
//ModbusRtuSlave modbusSlave = new ModbusRtuSlave("COM5", 9600, Parity.None, 8, StopBits.One, 1);

//try
//{
//    Console.WriteLine("Modbus RTU Slave運行中...");
//    while (true)
//    {
//        Thread.Sleep(1000); // 保持程序運行
//    }
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"錯誤: {ex.Message}");
//}
//finally
//{
//    modbusSlave.Close();
//}
ModbusRtuMaster modbusMaster = new ModbusRtuMaster("COM6", 9600, Parity.None, 8, StopBits.One);

try
{

    // 讀取保持寄存器
    byte[] response = modbusMaster.ReadHoldingRegisters(1, 0, 10);
    Console.WriteLine("保持寄存器數據:");
    foreach (byte b in response)
    {
        Console.Write($"{b:X2} ");
    }
    Console.WriteLine();


    // 寫入多個保持寄存器
    ushort[] data = new ushort[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    modbusMaster.WriteMultipleRegisters(1, 0, data);


    // 寫入單個保持寄存器
    modbusMaster.WriteSingleRegister(1, 0, 1234);
    // 讀取線圈
    byte[] coils = modbusMaster.ReadCoils(1, 0, 10);
    Console.WriteLine("線圈狀態:");
    foreach (byte b in coils)
    {
        Console.Write($"{b:X2} ");
    }
    Console.WriteLine();

    // 寫入多個線圈
    bool[] coilValues = new bool[] { true, false, true, false, true, false, true, false, true, false };
    modbusMaster.WriteMultipleCoils(1, 0, coilValues);
    
}
catch (Exception ex)
{
    Console.WriteLine($"錯誤: {ex.Message}");
}
finally
{
    modbusMaster.Close();
}