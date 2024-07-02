using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class ModbusRtuSlave
    {
        private readonly SerialPort _serialPort;
        private readonly byte _slaveAddress;
        private readonly bool[] _coils = new bool[10000];
        private readonly bool[] _discreteInputs = new bool[10000];
        private readonly ushort[] _holdingRegisters = new ushort[10000];
        private readonly ushort[] _inputRegisters = new ushort[10000];
        private readonly object _lock = new object(); // 用於同步的鎖對象

        public ModbusRtuSlave(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, byte slaveAddress)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 3000, // 讀取超時時間設定為3秒
                WriteTimeout = 3000 // 寫入超時時間設定為3秒
            };
            _slaveAddress = slaveAddress;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.Open();
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lock)
            {
                try
                {
                    int bytesToRead = _serialPort.BytesToRead;
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);

                    // 確認數據完整性和CRC校驗
                    if (buffer.Length >= 5)
                    {
                        byte slaveAddress = buffer[0];
                        byte functionCode = buffer[1];
                        ushort crc = CalculateCrc(buffer, buffer.Length - 2);

                        if (crc == (buffer[buffer.Length - 2] | (buffer[buffer.Length - 1] << 8)) && slaveAddress == _slaveAddress)
                        {
                            byte[] response = ProcessRequest(buffer, functionCode);
                            if (response != null)
                            {
                                _serialPort.Write(response, 0, response.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"錯誤: {ex.Message}");
                }
            }
        }

        private byte[] ProcessRequest(byte[] request, byte functionCode)
        {
            ushort startAddress = (ushort)((request[2] << 8) | request[3]);
            ushort quantity = (ushort)((request[4] << 8) | request[5]);

            switch (functionCode)
            {
                case 0x01:
                    return BuildReadCoilsResponse(request, startAddress, quantity);
                case 0x02:
                    return BuildReadDiscreteInputsResponse(request, startAddress, quantity);
                case 0x03:
                    return BuildReadHoldingRegistersResponse(request, startAddress, quantity);
                case 0x04:
                    return BuildReadInputRegistersResponse(request, startAddress, quantity);
                case 0x05:
                    return BuildWriteSingleCoilResponse(request, startAddress);
                case 0x06:
                    return BuildWriteSingleRegisterResponse(request, startAddress);
                case 0x0F:
                    return BuildWriteMultipleCoilsResponse(request, startAddress, quantity);
                case 0x10:
                    return BuildWriteMultipleRegistersResponse(request, startAddress, quantity);
                default:
                    return BuildErrorResponse(request, functionCode, 0x01); // 不合法的功能碼
            }
        }

        private byte[] BuildReadCoilsResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            int byteCount = (quantity + 7) / 8;
            byte[] response = new byte[5 + byteCount];
            response[0] = _slaveAddress;
            response[1] = 0x01;
            response[2] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                if (_coils[startAddress + i])
                    response[3 + i / 8] |= (byte)(1 << (i % 8));
            }

            ushort crc = CalculateCrc(response, response.Length - 2);
            response[response.Length - 2] = (byte)(crc & 0xFF);
            response[response.Length - 1] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildReadDiscreteInputsResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            int byteCount = (quantity + 7) / 8;
            byte[] response = new byte[5 + byteCount];
            response[0] = _slaveAddress;
            response[1] = 0x02;
            response[2] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                if (_discreteInputs[startAddress + i])
                    response[3 + i / 8] |= (byte)(1 << (i % 8));
            }

            ushort crc = CalculateCrc(response, response.Length - 2);
            response[response.Length - 2] = (byte)(crc & 0xFF);
            response[response.Length - 1] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildReadHoldingRegistersResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            byte[] response = new byte[5 + 2 * quantity];
            response[0] = _slaveAddress;
            response[1] = 0x03;
            response[2] = (byte)(2 * quantity);

            for (int i = 0; i < quantity; i++)
            {
                response[3 + 2 * i] = (byte)(_holdingRegisters[startAddress + i] >> 8);
                response[4 + 2 * i] = (byte)(_holdingRegisters[startAddress + i] & 0xFF);
            }

            ushort crc = CalculateCrc(response, response.Length - 2);
            response[response.Length - 2] = (byte)(crc & 0xFF);
            response[response.Length - 1] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildReadInputRegistersResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            byte[] response = new byte[5 + 2 * quantity];
            response[0] = _slaveAddress;
            response[1] = 0x04;
            response[2] = (byte)(2 * quantity);

            for (int i = 0; i < quantity; i++)
            {
                response[3 + 2 * i] = (byte)(_inputRegisters[startAddress + i] >> 8);
                response[4 + 2 * i] = (byte)(_inputRegisters[startAddress + i] & 0xFF);
            }

            ushort crc = CalculateCrc(response, response.Length - 2);
            response[response.Length - 2] = (byte)(crc & 0xFF);
            response[response.Length - 1] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildWriteSingleCoilResponse(byte[] request, ushort coilAddress)
        {
            bool value = request[4] == 0xFF00;

            _coils[coilAddress] = value;

            byte[] response = new byte[8];
            Array.Copy(request, response, request.Length);

            ushort crc = CalculateCrc(response, 6);
            response[6] = (byte)(crc & 0xFF);
            response[7] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildWriteSingleRegisterResponse(byte[] request, ushort registerAddress)
        {
            ushort value = (ushort)((request[4] << 8) | request[5]);

            _holdingRegisters[registerAddress] = value;

            byte[] response = new byte[8];
            Array.Copy(request, response, request.Length);

            ushort crc = CalculateCrc(response, 6);
            response[6] = (byte)(crc & 0xFF);
            response[7] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildWriteMultipleCoilsResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            int byteCount = request[6];
            for (int i = 0; i < quantity; i++)
            {
                bool value = (request[7 + i / 8] & (1 << (i % 8))) != 0;
                _coils[startAddress + i] = value;
            }

            byte[] response = new byte[8];
            Array.Copy(request, 0, response, 0, 6);

            ushort crc = CalculateCrc(response, 6);
            response[6] = (byte)(crc & 0xFF);
            response[7] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildWriteMultipleRegistersResponse(byte[] request, ushort startAddress, ushort quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                ushort value = (ushort)((request[7 + 2 * i] << 8) | request[8 + 2 * i]);
                _holdingRegisters[startAddress + i] = value;
            }

            byte[] response = new byte[8];
            Array.Copy(request, 0, response, 0, 6);

            ushort crc = CalculateCrc(response, 6);
            response[6] = (byte)(crc & 0xFF);
            response[7] = (byte)(crc >> 8);

            return response;
        }

        private byte[] BuildErrorResponse(byte[] request, byte functionCode, byte exceptionCode)
        {
            byte[] response = new byte[5];
            response[0] = _slaveAddress;
            response[1] = (byte)(functionCode | 0x80);
            response[2] = exceptionCode;

            ushort crc = CalculateCrc(response, 3);
            response[3] = (byte)(crc & 0xFF);
            response[4] = (byte)(crc >> 8);

            return response;
        }

        private ushort CalculateCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;

            for (int pos = 0; pos < length; pos++)
            {
                crc ^= (ushort)data[pos];

                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }
    }
}
