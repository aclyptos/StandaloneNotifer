using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml;
using StandaloneNotifier.VRCX.IPC;
using StandaloneNotifier.VRCX.IPC.Packets;

namespace StandaloneNotifier.VRCX.IPC
{
    internal class IPCClient
    {
        private static readonly UTF8Encoding noBomEncoding = new(false, false);

        private readonly byte[] packetBuffer = new byte[1024 * 1024];

        private Thread? _connectThread;
        private NamedPipeClientStream? _ipcClient;
        internal bool Connected => _ipcClient != null && _ipcClient.IsConnected;

        internal IPCClient()
        {
            var pingThread = new Thread(PingThread);
            pingThread.IsBackground = true;
            pingThread.Start();
        }

        private void PingThread()
        {
            var pingPacket = new PingPacket
            {
                Version = $"{Program.Version}"
            };
            while (true)
            {
                Thread.Sleep(1000);

                Write(pingPacket);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetCustomTag(string userId, string tag, string color)
        {
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.CustomTag)
            {
                UserId = userId,
                Tag = tag,
                TagColour = color
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendMessage(string mesage, string userId, string displayName)
        {
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.External)
            {
                Data = mesage,
                DisplayName = displayName,
                UserId = userId
            });
            Write(new VrcxMessagePacket(VrcxMessagePacket.MessageType.Noty)
            {
                Data = mesage
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Connect()
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

        private void ConnectThread()
        {
            var failedConnectionCount = 0;
            if (_ipcClient == null)
                return;
            while (true)
            {
                try
                {
                    _ipcClient.Connect(30000);

                    _connectThread = null;
                    Console.WriteLine("\0Connected to VRCX IPC server. Notifications will be sent via VRCX");

                    break;
                }
                catch (Exception e)
                {

                }

                Thread.Sleep(30000);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(string msg)
        {
            if (_ipcClient == null || !_ipcClient.IsConnected)
                return;

            using var memoryStream = new MemoryStream(packetBuffer);
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var streamWriter = new StreamWriter(memoryStream, noBomEncoding, 65535, true);

            streamWriter.Write(msg);
            streamWriter.Write((char)0x00);
            streamWriter.Flush();

            var length = (int)memoryStream.Position;

            _ipcClient?.BeginWrite(packetBuffer, 0, length, OnWrite, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(VrcxMessagePacket ipcPacket) => Write(JsonSerializer.Serialize(ipcPacket, VrcxMessagePacketContext.Default.VrcxMessagePacket));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(PingPacket ipcPacket) => Write(JsonSerializer.Serialize(ipcPacket, PingPacketContext.Default.PingPacket));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnWrite(IAsyncResult asyncResult)
        {
            try
            {
                _ipcClient?.EndWrite(asyncResult);
            }
            catch (Exception e)
            {
                Console.WriteLine("\0Lost connection to the VRCX IPC Server. Reconnecting...");
                Connect();
            }
        }
    }
}