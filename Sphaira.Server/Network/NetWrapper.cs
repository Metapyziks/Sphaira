﻿using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace Sphaira.Server.Network
{
    public static class NetWrapper
    {
        private static Dictionary<String, Action<NetIncomingMessage>> _handlersIdent =
            new Dictionary<string, Action<NetIncomingMessage>>();

        private static Dictionary<String, ushort> _indices = new Dictionary<String, ushort>();
        private static List<String> _idents = new List<String>();
        private static List<Action<NetIncomingMessage>> _handlers = new List<Action<NetIncomingMessage>>();

        private static NetServer _server;

        public static NetPeerStatus Status
        {
            get { return _server != null ? _server.Status : NetPeerStatus.NotRunning; }
        }

        public static List<NetConnection> Connections
        {
            get { return _server.Connections; }
        }

        public static void StartListening(int port, int maxConnections)
        {
            var config = new NetPeerConfiguration("Sphaira");
            config.MaximumConnections = maxConnections;
            config.Port = 14242;

            _server = new NetServer(config);
            _server.Start();

            Console.WriteLine("Started listening on port {0}", config.Port);
        }

        public static void SendMessage(String ident, Action<NetOutgoingMessage> builder,
            NetConnection recipient, NetDeliveryMethod method, int sequenceChannel = 0)
        {
            var msg = _server.CreateMessage();
            msg.Write(_indices[ident]);
            builder(msg);
            _server.SendMessage(msg, recipient, method, sequenceChannel);
        }

        public static void SendMessage(String ident, Action<NetOutgoingMessage> builder,
            List<NetConnection> recipients, NetDeliveryMethod method, int sequenceChannel = 0)
        {
            if (recipients.Count == 0) return;

            var msg = _server.CreateMessage();
            msg.Write(_indices[ident]);
            builder(msg);
            _server.SendMessage(msg, recipients, method, sequenceChannel);
        }

        public static void RegisterIdentifier(String ident)
        {
            if (!_idents.Contains(ident)) {
                _indices.Add(ident, (ushort) _idents.Count);
                _idents.Add(ident);
                _handlers.Add(null);
            } else {
                _handlers[_idents.IndexOf(ident)] = null;
            }
        }

        public static void RegisterMessageHandler(String ident, Action<NetIncomingMessage> handler)
        {
            if (!_handlersIdent.ContainsKey(ident)) {
                _handlersIdent.Add(ident, handler);
            } else {
                _handlersIdent[ident] = handler;
            }

            RegisterIdentifier(ident);
            _handlers[_idents.IndexOf(ident)] = handler;
        }

        private static void SendMessageTypes(NetOutgoingMessage msg)
        {
            msg.Write((ushort) _idents.Count);

            foreach (var ident in _idents) {
                msg.Write(ident);
            }
        }

        public static bool CheckForMessages()
        {
            bool received = false;

            NetIncomingMessage msg;
            while ((msg = _server.ReadMessage()) != null) {
                received = true;

                switch (msg.MessageType) {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        Console.WriteLine(msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus) msg.ReadByte();
                        var reason = msg.ReadString();

                        Console.WriteLine(String.Format("New status: {0} (Reason: {1})", status, reason));
                        break;
                    case NetIncomingMessageType.Data:
                        ushort id = msg.ReadUInt16();

                        if (id == 0xffff) {
                            var rsp = _server.CreateMessage();
                            rsp.Write(id);
                            SendMessageTypes(rsp);
                            _server.SendMessage(rsp, msg.SenderConnection,
                                NetDeliveryMethod.ReliableOrdered, 0);
                            break;
                        }
                        
                        if (id < _handlers.Count) {
                            _handlers[id](msg);
                            break;
                        }
                        
                        Console.WriteLine("Unhandled message type: {0}", id);
                        break;
                    default:
                        Console.WriteLine("Unhandled type: {0}", msg.MessageType);
                        break;
                }

                _server.Recycle(msg);
            }

            return received;
        }
    }
}
