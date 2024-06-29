using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class ModbusTcpServer
    {
        private readonly TcpListener _listener;
        private readonly bool[] _coils;
        private readonly bool[] _discreteInputs;
        private readonly ushort[] _holdingRegisters;
        private readonly ushort[] _inputRegisters;

        public ModbusTcpServer(string ipAddress, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _coils = new bool[10000]; // 假設我們有10000個線圈
            _discreteInputs = new bool[10000]; // 假設我們有10000個離散輸入
            _holdingRegisters = new ushort[10000]; // 假設我們有10000個保持寄存器
            _inputRegisters = new ushort[10000]; // 假設我們有10000個輸入寄存器
            InitializeRegisters();
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Modbus TCP Server is running...");
            while (true)
            {
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private void HandleClient(object obj)
        {
            var client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();

            while (true)
            {
                if (!client.Connected)
                    break;

                // 讀取請求
                byte[] request = new byte[256];
                int bytesRead = stream.Read(request, 0, request.Length);

                if (bytesRead > 0)
                {
                    // 處理請求並生成回應
                    byte[] response = ProcessRequest(request, bytesRead);

                    // 發送回應
                    stream.Write(response, 0, response.Length);
                }
            }

            client.Close();
        }

        private byte[] ProcessRequest(byte[] request, int bytesRead)
        {
            // 簡單的檢查請求格式是否正確
            if (bytesRead < 12)
                return BuildErrorResponse(request, 0x01); // 非法功能

            byte unitId = request[6];
            byte functionCode = request[7];
            ushort startAddress = (ushort)((request[8] << 8) + request[9]);
            ushort quantityOfRegisters = (ushort)((request[10] << 8) + request[11]);

            switch (functionCode)
            {
                case 0x01: // 讀取線圈
                    return BuildReadCoilsResponse(request, unitId, startAddress, quantityOfRegisters);
                case 0x02: // 讀取離散輸入
                    return BuildReadDiscreteInputsResponse(request, unitId, startAddress, quantityOfRegisters);
                case 0x03: // 讀取保持寄存器
                    return BuildReadHoldingRegistersResponse(request, unitId, startAddress, quantityOfRegisters);
                case 0x04: // 讀取輸入寄存器
                    return BuildReadInputRegistersResponse(request, unitId, startAddress, quantityOfRegisters);
                case 0x05: // 寫單個線圈
                    return BuildWriteSingleCoilResponse(request, unitId, startAddress);
                case 0x06: // 寫單個保持寄存器
                    return BuildWriteSingleRegisterResponse(request, unitId, startAddress);
                case 0x0F: // 寫多個線圈
                    return BuildWriteMultipleCoilsResponse(request, unitId, startAddress, quantityOfRegisters);
                case 0x10: // 寫多個保持寄存器
                    return BuildWriteMultipleRegistersResponse(request, unitId, startAddress, quantityOfRegisters);
                default:
                    return BuildErrorResponse(request, 0x01); // 非法功能
            }
        }

        private byte[] BuildReadCoilsResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfCoils)
        {
            int byteCount = (quantityOfCoils + 7) / 8;
            byte[] response = new byte[9 + byteCount];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = (byte)(3 + byteCount);

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x01;

            // Byte Count
            response[8] = (byte)byteCount;

            // Coil Status
            for (int i = 0; i < quantityOfCoils; i++)
            {
                if (_coils[startAddress + i])
                    response[9 + i / 8] |= (byte)(1 << (i % 8));
            }

            return response;
        }

        private byte[] BuildReadDiscreteInputsResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfInputs)
        {
            int byteCount = (quantityOfInputs + 7) / 8;
            byte[] response = new byte[9 + byteCount];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = (byte)(3 + byteCount);

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x02;

            // Byte Count
            response[8] = (byte)byteCount;

            // Discrete Input Status
            for (int i = 0; i < quantityOfInputs; i++)
            {
                if (_discreteInputs[startAddress + i])
                    response[9 + i / 8] |= (byte)(1 << (i % 8));
            }

            return response;
        }

        private byte[] BuildReadHoldingRegistersResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfRegisters)
        {
            byte[] response = new byte[9 + quantityOfRegisters * 2];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = (byte)(3 + quantityOfRegisters * 2);

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x03;

            // Byte Count
            response[8] = (byte)(quantityOfRegisters * 2);

            // Register Values
            for (int i = 0; i < quantityOfRegisters; i++)
            {
                ushort value = _holdingRegisters[startAddress + i];
                response[9 + i * 2] = (byte)(value >> 8);
                response[10 + i * 2] = (byte)(value & 0xFF);
            }

            return response;
        }

        private byte[] BuildReadInputRegistersResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfRegisters)
        {
            byte[] response = new byte[9 + quantityOfRegisters * 2];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = (byte)(3 + quantityOfRegisters * 2);

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x04;

            // Byte Count
            response[8] = (byte)(quantityOfRegisters * 2);

            // Register Values
            for (int i = 0; i < quantityOfRegisters; i++)
            {
                ushort value = _inputRegisters[startAddress + i];
                response[9 + i * 2] = (byte)(value >> 8);
                response[10 + i * 2] = (byte)(value & 0xFF);
            }

            return response;
        }

        private byte[] BuildWriteSingleCoilResponse(byte[] request, byte unitId, ushort coilAddress)
        {
            bool coilValue = request[10] == 0xFF;

            _coils[coilAddress] = coilValue;

            byte[] response = new byte[12];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 12);

            return response;
        }

        private byte[] BuildWriteSingleRegisterResponse(byte[] request, byte unitId, ushort registerAddress)
        {
            ushort registerValue = (ushort)((request[10] << 8) + request[11]);

            _holdingRegisters[registerAddress] = registerValue;

            byte[] response = new byte[12];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 12);

            return response;
        }

        private byte[] BuildWriteMultipleCoilsResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfCoils)
        {
            int byteCount = request[12];
            byte[] response = new byte[12];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = 0x06;

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x0F;

            // Starting Address
            response[8] = request[8];
            response[9] = request[9];

            // Quantity of Coils
            response[10] = request[10];
            response[11] = request[11];

            // Update Coils
            for (int i = 0; i < quantityOfCoils; i++)
            {
                bool coilValue = (request[13 + i / 8] & (1 << (i % 8))) != 0;
                _coils[startAddress + i] = coilValue;
            }

            return response;
        }

        private byte[] BuildWriteMultipleRegistersResponse(byte[] request, byte unitId, ushort startAddress, ushort quantityOfRegisters)
        {
            byte[] response = new byte[12];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = 0x06;

            // Unit Identifier
            response[6] = unitId;

            // Function Code
            response[7] = 0x10;

            // Starting Address
            response[8] = request[8];
            response[9] = request[9];

            // Quantity of Registers
            response[10] = request[10];
            response[11] = request[11];

            // Update Registers
            for (int i = 0; i < quantityOfRegisters; i++)
            {
                ushort value = (ushort)((request[13 + i * 2] << 8) + request[14 + i * 2]);
                _holdingRegisters[startAddress + i] = value;
            }

            return response;
        }

        private byte[] BuildErrorResponse(byte[] request, byte errorCode)
        {
            byte[] response = new byte[9];

            // Transaction Identifier
            Buffer.BlockCopy(request, 0, response, 0, 4);

            // Length
            response[4] = 0x00;
            response[5] = 0x03;

            // Unit Identifier
            response[6] = request[6];

            // Function Code (with error flag)
            response[7] = (byte)(request[7] | 0x80);

            // Error Code
            response[8] = errorCode;

            return response;
        }

        private void InitializeRegisters()
        {
            for (int i = 0; i < _holdingRegisters.Length; i++)
            {
                _holdingRegisters[i] = (ushort)(i + 1); // 初始化保持寄存器值
            }

            for (int i = 0; i < _inputRegisters.Length; i++)
            {
                _inputRegisters[i] = (ushort)(i + 1); // 初始化輸入寄存器值
            }

            for (int i = 0; i < _coils.Length; i++)
            {
                _coils[i] = (i % 2 == 0); // 初始化線圈值
            }

            for (int i = 0; i < _discreteInputs.Length; i++)
            {
                _discreteInputs[i] = (i % 2 == 0); // 初始化離散輸入值
            }
        }
    }
}
