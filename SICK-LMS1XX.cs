/*
 * A C#.NET class to communicate with SICK SENSOR LMS1xx
 * 
 * Author : beolabs.io / Benjamin Oms
 * Update : 12/06/2017
 * Github : https://github.com/beolabs-io/Coinmarketcap-APIv2
 * 
 * --- MIT LICENCE ---
 * 
 * Copyright (c) 2017 beolabs.io
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BSICK.Sensors.LMS1xx
{
    public class LMS1XX
    {
        #region Enumérations

        public enum SocketConnectionResult { CONNECTED = 0, CONNECT_TIMEOUT = 1, CONNECT_ERROR = 2, DISCONNECTED = 3, DISCONNECT_TIMEOUT = 4, DISCONNECT_ERROR = 5 }
        public enum NetworkStreamResult    { STARTED = 0, STOPPED = 1, TIMEOUT = 2, ERROR = 3, CLIENT_NOT_CONNECTED = 4 }

        #endregion

        #region Propriétés publiques

        public String IpAddress      { get; set; }
        public int    Port           { get; set; }
        public int    ReceiveTimeout { get; set; }
        public int    SendTimeout    { get; set; }

        #endregion

        #region Propriétés privées

        private TcpClient clientSocket;

        #endregion

        #region Constructeurs

        public LMS1XX()
        {

            this.clientSocket = new TcpClient() { ReceiveTimeout = 1000, SendTimeout = 1000 };
            this.IpAddress    = String.Empty;
            this.Port         = 0;
        }

        public LMS1XX(string ipAdress, int port, int receiveTimeout, int sendTimeout)
        {
            this.clientSocket = new TcpClient() { ReceiveTimeout = receiveTimeout, SendTimeout = sendTimeout } ;
            this.IpAddress    = ipAdress;
            this.Port         = port;
        }

        #endregion

        #region Methodes de base pour le pilotage du capteur

        public bool IsSocketConnected()
        {
            return clientSocket.Connected;
        }

        public SocketConnectionResult Connect()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.DISCONNECTED)
            {
                try
                {
                    clientSocket.Connect(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.CONNECT_TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException)  { status = SocketConnectionResult.CONNECT_ERROR;   this.Disconnect(); return status; }
            }
            return status;
        }

        public async Task<SocketConnectionResult> ConnectAsync()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.DISCONNECTED)
            {
                try
                {
                    await clientSocket.ConnectAsync(this.IpAddress, this.Port);
                    status = SocketConnectionResult.CONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.CONNECT_TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException)  { status = SocketConnectionResult.CONNECT_ERROR;   this.Disconnect(); return status; }
            }
            return status;
        }

        public SocketConnectionResult Disconnect()
        {
            SocketConnectionResult status = (clientSocket.Connected) ? SocketConnectionResult.CONNECTED : SocketConnectionResult.DISCONNECTED;
            if (status == SocketConnectionResult.CONNECTED)
            {
                try
                {
                    clientSocket.Close();
                    clientSocket = new TcpClient() { ReceiveTimeout = this.ReceiveTimeout };
                    status = SocketConnectionResult.DISCONNECTED;
                }
                catch (TimeoutException) { status = SocketConnectionResult.DISCONNECT_TIMEOUT; return status; }
                catch (SystemException)  { status = SocketConnectionResult.DISCONNECT_ERROR;   return status; }
            }
            return status;
        }

        public NetworkStreamResult Start()
        {
            byte[] cmd = new byte[18] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x61, 0x72, 0x74, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();
                    serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STARTED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException ) { status = NetworkStreamResult.ERROR;   this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }

        public async Task<NetworkStreamResult> StartAsync()
        {
            byte[] cmd = new byte[18] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x61, 0x72, 0x74, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();
                    await serverStream.WriteAsync(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STARTED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException)  { status = NetworkStreamResult.ERROR;   this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }

        public NetworkStreamResult Stop()
        {
            byte[] cmd = new byte[17] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x6F, 0x70, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();

                    serverStream.Write(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STOPPED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException)  { status = NetworkStreamResult.ERROR;   this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }
            
            return status;
        }

        public async Task<NetworkStreamResult> StopAsync()
        {
            byte[] cmd = new byte[17] { 0x02, 0x73, 0x4D, 0x4E, 0x20, 0x4C, 0x4D, 0x43, 0x73, 0x74, 0x6F, 0x70, 0x6D, 0x65, 0x61, 0x73, 0x03 };

            NetworkStreamResult status;
            if (clientSocket.Connected)
            {
                try
                {
                    NetworkStream serverStream = clientSocket.GetStream();

                    await serverStream.WriteAsync(cmd, 0, cmd.Length);
                    status = NetworkStreamResult.STOPPED;
                }
                catch (TimeoutException) { status = NetworkStreamResult.TIMEOUT; this.Disconnect(); return status; }
                catch (SystemException)  { status = NetworkStreamResult.ERROR;   this.Disconnect(); return status; }
            }
            else
            {
                status = NetworkStreamResult.CLIENT_NOT_CONNECTED;
            }

            return status;
        }

        public byte[] ExecuteRaw(byte[] streamCommand)
        {
            try
            {
                NetworkStream serverStream = clientSocket.GetStream();
                serverStream.Write(streamCommand, 0, streamCommand.Length);
                serverStream.Flush();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);

                return inStream;
            }
            catch(Exception ex)
            {
                return null;
            } 
        }

        public async Task<byte[]> ExecuteRawAsync(byte[] streamCommand)
        {
            try
            {
                NetworkStream serverStream = clientSocket.GetStream();
                await serverStream.WriteAsync(streamCommand, 0, streamCommand.Length);
                await serverStream.FlushAsync();

                byte[] inStream = new byte[clientSocket.ReceiveBufferSize];
                await serverStream.ReadAsync(inStream, 0, (int)clientSocket.ReceiveBufferSize);

                return inStream;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public struct SetAccessModeResult
        {
            public byte[] RawData;
        }

        public SetAccessModeResult SetAccessMode()
        {
            SetAccessModeResult result;
            byte[] command = new byte[] { 0x02, 0x73, 0x41, 0x4E, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4D, 0x6F, 0x64, 0x65, 0x20, 0x31, 0x03 };
            result.RawData = this.ExecuteRaw(command);
            return result;
        }

        public async Task<SetAccessModeResult> SetAccessModeAsync()
        {
            SetAccessModeResult result;
            byte[] command = new byte[] { 0x02, 0x73, 0x41, 0x4E, 0x20, 0x53, 0x65, 0x74, 0x41, 0x63, 0x63, 0x65, 0x73, 0x73, 0x4D, 0x6F, 0x64, 0x65, 0x20, 0x31, 0x03 };
            result.RawData = await this.ExecuteRawAsync(command);
            return result;
        }

        public struct LMDScandataResult
        {
            public bool         IsError;
            public Exception    ErrorException;
            public byte[]       RawData;
            public String       RawDataString;
            public String       CommandType;
            public String       Command;
            public int?         VersionNumber;
            public int?         DeviceNumber;
            public int?         SerialNumber;
            public String       DeviceStatus;
            public int?         TelegramCounter;
            public int?         ScanCounter;
            public uint?        TimeSinceStartup;
            public uint?        TimeOfTransmission;
            public String       StatusOfDigitalInputs;
            public String       StatusOfDigitalOutputs;
            public int?         Reserved;
            public double?      ScanFrequency;
            public double?      MeasurementFrequency;
            public int?         AmountOfEncoder;
            public int?         EncoderPosition;
            public int?         EncoderSpeed;
            public int?         AmountOf16BitChannels;
            public String       Content;
            public String       ScaleFactor;
            public String       ScaleFactorOffset;
            public double?      StartAngle;
            public double?      SizeOfSingleAngularStep;
            public int?         AmountOfData;
            public List<double> DistancesData;

            public LMDScandataResult(byte [] rawData)
            {
                IsError                 = true;
                ErrorException          = null;
                RawData                 = rawData;
                RawDataString           = Encoding.ASCII.GetString(rawData);
                DistancesData           = new List<double>();
                CommandType             = String.Empty;
                Command                 = String.Empty;
                VersionNumber           = null;
                DeviceNumber            = null;
                SerialNumber            = null;
                DeviceStatus            = String.Empty;
                TelegramCounter         = null;
                ScanCounter             = null;
                TimeSinceStartup        = null;
                TimeOfTransmission      = null;
                StatusOfDigitalInputs   = String.Empty;
                StatusOfDigitalOutputs  = String.Empty;
                Reserved                = null;
                ScanFrequency           = null;
                MeasurementFrequency    = null;
                AmountOfEncoder         = null;
                EncoderPosition         = null;
                EncoderSpeed            = null;
                AmountOf16BitChannels   = null;
                Content                 = String.Empty;
                ScaleFactor             = String.Empty;
                ScaleFactorOffset       = String.Empty;
                StartAngle              = null;
                SizeOfSingleAngularStep = null;
                AmountOfData            = null;
            }
        }

        public LMDScandataResult LMDScandata()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4C, 0x4D, 0x44, 0x73, 0x63, 0x61, 0x6E, 0x64, 0x61, 0x74, 0x61, 0x03 };

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = this.ExecuteRaw(command);
                }
                catch(Exception ex)
                {
                    return new LMDScandataResult() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    LMDScandataResult result = new LMDScandataResult(rawData);
                    result.IsError           = false;
                    result.ErrorException    = null;

                    int dataIndex       = 0;
                    int dataBlocCounter = 0;
                    string dataBloc     = String.Empty;

                    while (dataBlocCounter < 28)
                    {
                        dataIndex++;
                        if ((dataIndex < result.RawDataString.Length) && !(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            ++dataBlocCounter;
                            switch (dataBlocCounter)
                            {
                                case 1: result.CommandType              = dataBloc; break;
                                case 2: result.Command                  = dataBloc; break;
                                case 3: result.VersionNumber            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 4: result.DeviceNumber             = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 5: result.SerialNumber             = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 6: result.DeviceStatus             = dataBloc; break;
                                case 7: result.DeviceStatus            += "-" + dataBloc; break;
                                case 8: result.TelegramCounter          = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 9: result.ScanCounter              = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 10: result.TimeSinceStartup        = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 11: result.TimeOfTransmission      = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 12: result.StatusOfDigitalInputs   = dataBloc; break;
                                case 13: result.StatusOfDigitalInputs  += "-" + dataBloc; break;
                                case 14: result.StatusOfDigitalOutputs  = dataBloc; break;
                                case 15: result.StatusOfDigitalOutputs += "-" + dataBloc; break;
                                case 16: result.Reserved                = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 17: result.ScanFrequency           = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 100; break;
                                case 18: result.MeasurementFrequency    = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10; break;
                                case 19: result.AmountOfEncoder         = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); if (result.AmountOfEncoder <= 0) dataBlocCounter += 2; break;
                                case 20: result.EncoderPosition         = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 21: result.EncoderSpeed            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 22: result.AmountOf16BitChannels   = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 23: result.Content                 = dataBloc; break;
                                case 24: result.ScaleFactor             = dataBloc; break;
                                case 25: result.ScaleFactorOffset       = dataBloc; break;
                                case 26: result.StartAngle              = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 27: result.SizeOfSingleAngularStep = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 28: result.AmountOfData            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                            }
                            dataBloc = String.Empty;
                            if (result.CommandType != "sRA") return result;
                        }
                    }

                    dataBloc = String.Empty;
                    while (dataBlocCounter < result.AmountOfData + 28)
                    {
                        ++dataIndex;
                        if (!(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            result.DistancesData.Add(Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 1000);
                            dataBloc = String.Empty;
                            ++dataBlocCounter;
                        }
                    }

                    return result;
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Raw data is null.") };
            }
            else
                return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
        }

        public async Task<LMDScandataResult> LMDScandataAsync()
        {
            byte[] command = new byte[] { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4C, 0x4D, 0x44, 0x73, 0x63, 0x61, 0x6E, 0x64, 0x61, 0x74, 0x61, 0x03 };

            if (clientSocket.Connected)
            {
                byte[] rawData = null;
                try
                {
                    rawData = await this.ExecuteRawAsync(command);
                }
                catch(Exception ex)
                {
                    return new LMDScandataResult() { IsError = true, ErrorException = ex };
                }

                if (rawData != null)
                {
                    LMDScandataResult result = new LMDScandataResult(rawData);
                    result.IsError           = false;
                    result.ErrorException    = null;

                    int dataIndex       = 0;
                    int dataBlocCounter = 0;
                    string dataBloc     = String.Empty;

                    while (dataBlocCounter < 28)
                    {
                        dataIndex++;
                        if ((dataIndex < result.RawDataString.Length) && !(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            ++dataBlocCounter;
                            switch (dataBlocCounter)
                            {
                                case 1: result.CommandType              = dataBloc; break;
                                case 2: result.Command                  = dataBloc; break;
                                case 3: result.VersionNumber            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 4: result.DeviceNumber             = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 5: result.SerialNumber             = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 6: result.DeviceStatus             = dataBloc; break;
                                case 7: result.DeviceStatus            += "-" + dataBloc; break;
                                case 8: result.TelegramCounter          = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 9: result.ScanCounter              = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 10: result.TimeSinceStartup        = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 11: result.TimeOfTransmission      = uint.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber) / 1000000; break;
                                case 12: result.StatusOfDigitalInputs   = dataBloc; break;
                                case 13: result.StatusOfDigitalInputs  += "-" + dataBloc; break;
                                case 14: result.StatusOfDigitalOutputs  = dataBloc; break;
                                case 15: result.StatusOfDigitalOutputs += "-" + dataBloc; break;
                                case 16: result.Reserved                = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 17: result.ScanFrequency           = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 100; break;
                                case 18: result.MeasurementFrequency    = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10; break;
                                case 19: result.AmountOfEncoder         = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); if (result.AmountOfEncoder <= 0) dataBlocCounter += 2; break;
                                case 20: result.EncoderPosition         = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 21: result.EncoderSpeed            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 22: result.AmountOf16BitChannels   = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                                case 23: result.Content                 = dataBloc; break;
                                case 24: result.ScaleFactor             = dataBloc; break;
                                case 25: result.ScaleFactorOffset       = dataBloc; break;
                                case 26: result.StartAngle              = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 27: result.SizeOfSingleAngularStep = Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 10000; break;
                                case 28: result.AmountOfData            = int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber); break;
                            }
                            dataBloc = String.Empty;
                            if (result.CommandType != "sRA")
                                return result;
                        }
                    }

                    dataBloc = String.Empty;
                    while (dataBlocCounter < result.AmountOfData + 28)
                    {
                        ++dataIndex;
                        if (!(result.RawDataString[dataIndex].ToString() == " "))
                        {
                            dataBloc += result.RawDataString[dataIndex];
                        }
                        else
                        {
                            result.DistancesData.Add(Convert.ToDouble(int.Parse(dataBloc, System.Globalization.NumberStyles.HexNumber)) / 1000);
                            dataBloc = String.Empty;
                            ++dataBlocCounter;
                        }
                    }

                    return result;
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Raw data is null.") };
            }
            else
                return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
        }

        #endregion

        #region Relevé Asynchrone des données du Capteur

        public async Task<LMDScandataResult> LMDScandataFullModeAsync()
        {
            try
            {
                LMDScandataResult scandataResult;

                var connectionResult = await this.ConnectAsync();
                if (connectionResult == SocketConnectionResult.CONNECTED)
                {
                    var networkStreamResult = await this.StartAsync();
                    if (networkStreamResult == NetworkStreamResult.STARTED)
                    {
                        scandataResult = await this.LMDScandataAsync(); // TO FIX: First call doesn't return data ?
                        scandataResult = await this.LMDScandataAsync(); // TO FIX: Second call return datas

                        if (!scandataResult.IsError)
                        {
                            networkStreamResult = await this.StopAsync();
                            if(networkStreamResult == NetworkStreamResult.STOPPED)
                            {
                                this.Disconnect();
                                return scandataResult; 
                            }
                            else
                            {
                                this.Disconnect();
                                scandataResult.IsError = true;
                                scandataResult.ErrorException = new Exception(string.Format("{0} Network stream improperly stopped.", scandataResult.ErrorException));
                                return scandataResult;
                            }
                        }
                        else
                        {
                            networkStreamResult = await this.StopAsync();
                            if (networkStreamResult == NetworkStreamResult.STOPPED)
                            {
                                this.Disconnect();
                                return scandataResult;
                            }
                            else
                            {
                                this.Disconnect();
                                scandataResult.IsError = true;
                                scandataResult.ErrorException = new Exception(string.Format("{0} Network stream improperly stopped.", scandataResult.ErrorException));
                                return scandataResult;
                            }
                        }
                    }
                    else
                        return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Network stream not started.") };
                }
                else
                    return new LMDScandataResult() { IsError = true, ErrorException = new Exception("Client socket not connected.") };
            }
            catch(Exception ex)
            {
                return new LMDScandataResult() { IsError = true, ErrorException = ex };
            }
        }

        #endregion
    }
}
