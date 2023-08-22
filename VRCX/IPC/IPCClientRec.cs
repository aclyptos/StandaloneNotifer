using StandaloneNotifier.VRCX.IPC.Packets;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StandaloneNotifier.VRCX.IPC
{
    public class IPCClientReceive
    {
        private readonly byte[] _recvBuffer = new byte[1024 * 8];
        private string _currentPacket;
        private Thread? _connectThread;
        private NamedPipeClientStream? _ipcClient;

        public void Connect(bool clear = true)
        {
            if (_connectThread != null)
                return;

            _ipcClient?.Dispose();
            _ipcClient = new NamedPipeClientStream(".", 
                "vrcx-ipc", PipeDirection.InOut);

            _connectThread = new Thread(ConnectThread);
            _connectThread.IsBackground = true;
            _connectThread.Start();
        }

        public void ConnectThread()
        {
            if (_ipcClient == null)
                return;
            while (true)
            {
                try
                {
                    _ipcClient.Connect(30000);
                    BeginRead();

                    _connectThread = null;
                    Console.WriteLine("Receiver connected to VRCX IPC server.");
                    break;
                }
                catch (Exception e)
                {
                }

                Thread.Sleep(30000);
            }
        }

        public void Disconnect()
        {
            _ipcClient?.Close();
        }

        public void BeginRead()
        {
            _ipcClient.BeginRead(_recvBuffer, 0, _recvBuffer.Length, OnRead, _ipcClient);
        }

        private void OnRead(IAsyncResult asyncResult)
        {
            try
            {
                var bytesRead = _ipcClient.EndRead(asyncResult);

                if (bytesRead <= 0)
                {
                    _ipcClient.Close();
                    return;
                }

                _currentPacket += Encoding.UTF8.GetString(_recvBuffer, 0, bytesRead);

                if (_currentPacket[_currentPacket.Length - 1] == (char)0x00)
                {
                    var packets = _currentPacket.Split((char)0x00);

                    foreach (var packet in packets)
                    {
                        if (string.IsNullOrEmpty(packet))
                            continue;

                        try
                        {
#if DEBUG
                            Console.WriteLine(packet);
#endif
                            RecPackage recPackage = JsonSerializer.Deserialize(packet, RecPackageContext.Default.RecPackage);

                            if (recPackage == null) continue;

#if DEBUG
                            Console.WriteLine(recPackage.Type);
#endif

                            if (recPackage.Type != "VrcxMessage") continue;

#if DEBUG
                            Console.WriteLine(recPackage.MsgType);
#endif

                            if(recPackage.MsgType == "ShowUserDialog")
                            {
#if DEBUG
                                Console.WriteLine(recPackage.Data);
#endif
                                Program.HandleJoin(null, recPackage.Data);
                            }
                        }
                        catch { }
                    }

                    _currentPacket = string.Empty;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            BeginRead();
        }
    }
}