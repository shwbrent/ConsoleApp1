using System.IO.Ports;

namespace ConsoleApp1
{
    public class ModbusRtuMaster
    {
        private readonly SerialPort _serialPort;
        private readonly object _lock = new object(); // 用於同步的鎖對象

        public ModbusRtuMaster(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 3000, // 讀取超時時間設定為3秒
                WriteTimeout = 3000 // 寫入超時時間設定為3秒
            };
            _serialPort.Open();
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public byte[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] frame = BuildReadRequestFrame(slaveAddress, 0x01, startAddress, numberOfPoints);
            byte[] response = SendAndReceive(frame);
            return ParseReadResponse(response);
        }

        public byte[] ReadDiscreteInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] frame = BuildReadRequestFrame(slaveAddress, 0x02, startAddress, numberOfPoints);
            byte[] response = SendAndReceive(frame);
            return ParseReadResponse(response);
        }

        public byte[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] frame = BuildReadRequestFrame(slaveAddress, 0x03, startAddress, numberOfPoints);
            byte[] response = SendAndReceive(frame);
            return ParseReadResponse(response);
        }

        public byte[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            byte[] frame = BuildReadRequestFrame(slaveAddress, 0x04, startAddress, numberOfPoints);
            byte[] response = SendAndReceive(frame);
            return ParseReadResponse(response);
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            byte[] frame = BuildWriteSingleCoilFrame(slaveAddress, coilAddress, value);
            SendAndReceive(frame);
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            byte[] frame = BuildWriteSingleRegisterFrame(slaveAddress, registerAddress, value);
            SendAndReceive(frame);
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] values)
        {
            byte[] frame = BuildWriteMultipleCoilsFrame(slaveAddress, startAddress, values);
            SendAndReceive(frame);
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] values)
        {
            byte[] frame = BuildWriteMultipleRegistersFrame(slaveAddress, startAddress, values);
            SendAndReceive(frame);
        }

        private byte[] BuildReadRequestFrame(byte slaveAddress, byte functionCode, ushort startAddress, ushort numberOfPoints)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = functionCode;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(numberOfPoints >> 8);
            frame[5] = (byte)(numberOfPoints & 0xFF);
            ushort crc = CalculateCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] BuildWriteSingleCoilFrame(byte slaveAddress, ushort coilAddress, bool value)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x05;
            frame[2] = (byte)(coilAddress >> 8);
            frame[3] = (byte)(coilAddress & 0xFF);
            frame[4] = value ? (byte)0xFF : (byte)0x00;
            frame[5] = 0x00;
            ushort crc = CalculateCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] BuildWriteSingleRegisterFrame(byte slaveAddress, ushort registerAddress, ushort value)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x06;
            frame[2] = (byte)(registerAddress >> 8);
            frame[3] = (byte)(registerAddress & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);
            ushort crc = CalculateCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] BuildWriteMultipleCoilsFrame(byte slaveAddress, ushort startAddress, bool[] values)
        {
            int byteCount = (values.Length + 7) / 8;
            byte[] frame = new byte[9 + byteCount];
            frame[0] = slaveAddress;
            frame[1] = 0x0F;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(values.Length >> 8);
            frame[5] = (byte)(values.Length & 0xFF);
            frame[6] = (byte)byteCount;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                    frame[7 + i / 8] |= (byte)(1 << (i % 8));
            }

            ushort crc = CalculateCrc(frame, 7 + byteCount);
            frame[7 + byteCount] = (byte)(crc & 0xFF);
            frame[8 + byteCount] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] BuildWriteMultipleRegistersFrame(byte slaveAddress, ushort startAddress, ushort[] values)
        {
            int byteCount = values.Length * 2;
            byte[] frame = new byte[9 + byteCount];
            frame[0] = slaveAddress;
            frame[1] = 0x10;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(values.Length >> 8);
            frame[5] = (byte)(values.Length & 0xFF);
            frame[6] = (byte)byteCount;

            for (int i = 0; i < values.Length; i++)
            {
                frame[7 + i * 2] = (byte)(values[i] >> 8);
                frame[8 + i * 2] = (byte)(values[i] & 0xFF);
            }

            ushort crc = CalculateCrc(frame, 7 + byteCount);
            frame[7 + byteCount] = (byte)(crc & 0xFF);
            frame[8 + byteCount] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] SendAndReceive(byte[] frame)
        {
            lock (_lock)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.Write(frame, 0, frame.Length);

                Thread.Sleep(100); // 等待設備回應

                byte[] buffer = new byte[256];
                int bytesRead = 0;

                try
                {
                    bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                    throw new Exception("接收數據超時");
                }

                byte[] response = new byte[bytesRead];
                Array.Copy(buffer, response, bytesRead);

                return response;
            }
        }

        private byte[] ParseReadResponse(byte[] response)
        {
            // 簡單解析，未進行詳細錯誤處理
            if (response.Length < 5)
            {
                throw new Exception("回應長度無效");
            }

            byte[] data = new byte[response[2]];
            Array.Copy(response, 3, data, 0, data.Length);
            return data;
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
