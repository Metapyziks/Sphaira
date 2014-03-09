using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Sphaira.Client.Network
{
    public static class NetWrapper
    {
        private static Dictionary<String, Action<NetIncomingMessage>> _handlersIdent =
            new Dictionary<string, Action<NetIncomingMessage>>();

        private static Dictionary<String, ushort> _indices =
            new Dictionary<String, ushort>();

        private static Action<NetIncomingMessage>[] _handlers;

        private static NetClient _client;
        private static NetConnection _connection;

        public static NetConnectionStatus Status
        {
            get { return _connection != null ? _connection.Status : NetConnectionStatus.Disconnected; }
        }

        public static void Connect(String hostname, int port)
        {
            var config = new NetPeerConfiguration("Sphaira");

            _client = new NetClient(config);
            _client.Start();

            _connection = _client.Connect(hostname, port);

            Trace.WriteLine(String.Format("Connecting to {0}", _connection.RemoteEndPoint));

            while (Status != NetConnectionStatus.Connected) {
                if (!CheckForMessages()) Thread.Sleep(16);
            }

            RequestMessageTypes();
        }

        public static void Disconnect()
        {
            _client.Disconnect("Quit");

            _client = null;
            _connection = null;
        }

        public static void SendMessage(String ident, NetDeliveryMethod method, int sequenceChannel = 0)
        {
            var msg = _client.CreateMessage();
            msg.Write(_indices[ident]);
            _client.SendMessage(msg, method, sequenceChannel);
        }

        public static void SendMessage(String ident, Action<NetOutgoingMessage> builder,
            NetDeliveryMethod method, int sequenceChannel = 0)
        {
            var msg = _client.CreateMessage();
            msg.Write(_indices[ident]);
            builder(msg);
            _client.SendMessage(msg, method, sequenceChannel);
        }

        public static void RegisterMessageHandler(String ident, Action<NetIncomingMessage> handler)
        {
            if (!_handlersIdent.ContainsKey(ident)) {
                _handlersIdent.Add(ident, handler);
            } else {
                _handlersIdent[ident] = handler;
            }

            if (_indices.ContainsKey(ident)) {
                _handlers[_indices[ident]] = handler;
            }
        }

        private static void RequestMessageTypes()
        {
            var msg = _client.CreateMessage();
            msg.Write((ushort) 0xffff);
            _client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);

            _handlers = null;

            while (_handlers == null) {
                if (!CheckForMessages()) Thread.Sleep(16);
            }
        }

        private static void ReceiveMessageTypes(NetIncomingMessage msg)
        {
            _handlers = new Action<NetIncomingMessage>[msg.ReadUInt16()];
            _indices.Clear();

            for (ushort i = 0; i < _handlers.Length; ++i) {
                var ident = msg.ReadString();

                _indices.Add(ident, i);

                if (_handlersIdent.ContainsKey(ident)) {
                    _handlers[i] = _handlersIdent[ident];
                }
            }
        }

        public static bool CheckForMessages()
        {
            bool received = false;

            NetIncomingMessage msg;
            while ((msg = _client.ReadMessage()) != null) {
                received = true;

                switch (msg.MessageType) {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        Trace.WriteLine(msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus) msg.ReadByte();
                        var reason = msg.ReadString();

                        Trace.WriteLine(String.Format("New status: {0} (Reason: {1})", status, reason));
                        break;
                    case NetIncomingMessageType.Data:
                        ushort id = msg.ReadUInt16();

                        if (id == 0xffff) {
                            ReceiveMessageTypes(msg);
                            break;
                        }

                        if (_handlers != null && id < _handlers.Length) {
                            var handler = _handlers[id];
                            if (handler != null) {
                                handler(msg);
                                break;
                            }
                        }

                        Trace.WriteLine(String.Format("Unhandled message type: {0}", id));
                        break;
                    default:
                        Trace.WriteLine(String.Format("Unhandled type: {0}", msg.MessageType));
                        break;
                }

                _client.Recycle(msg);
            }

            return received;
        }
    }
}
