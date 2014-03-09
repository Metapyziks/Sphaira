using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using OpenTK;
using Sphaira.Server.Network;

namespace Sphaira.Server
{
    public class Program
    {
        private class PlayerInfo
        {
            public ushort ID { get; private set; }

            public NetConnection Connection { get; private set; }

            public Vector3 Position { get; set; }

            public PlayerInfo(ushort id, NetConnection connection)
            {
                ID = id;
                Connection = connection;
            }
        }

        private static int _skySeed;
        private static Dictionary<NetConnection, PlayerInfo> _players;

        private static PlayerInfo GetPlayer(NetConnection connection)
        {
            return _players.Values.FirstOrDefault(x => x.Connection == connection) ?? AddPlayer(connection);
        }

        private static PlayerInfo AddPlayer(NetConnection connection)
        {
            ushort id = (ushort) Enumerable.Range(0, _players.Count + 1)
                .First(x => !_players.Values.Any(y => y.ID == x));

            var player = new PlayerInfo(id, connection);

            _players.Add(connection, player);

            UpdatePlayers(NetWrapper.Connections);

            return player;
        }

        private static void UpdatePlayers(List<NetConnection> recipients)
        {
            NetWrapper.SendMessage("PlayerInfo", msg => {
                msg.Write((ushort) _players.Count);

                foreach (var player in _players.Values) {
                    msg.Write(player.ID);
                }
            }, recipients, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static int Main(String[] args)
        {
            var rand = new Random();

            _skySeed = rand.Next(1, int.MaxValue);
            _players = new Dictionary<NetConnection, PlayerInfo>();

            NetWrapper.RegisterIdentifier("PlayerInfo");

            NetWrapper.RegisterMessageHandler("WorldInfo", msg => {
                var player = GetPlayer(msg.SenderConnection);
                NetWrapper.SendMessage("WorldInfo", reply => {
                    reply.Write(_skySeed);
                    reply.Write(player.ID);
                }, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
            });

            NetWrapper.RegisterMessageHandler("PlayerPos", msg => {
                var player = GetPlayer(msg.SenderConnection);
                player.Position = new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());

                var others = NetWrapper.Connections;
                others.Remove(msg.SenderConnection);

                NetWrapper.SendMessage("PlayerPos", reply => {
                    reply.Write(player.ID);
                    reply.Write(player.Position.X);
                    reply.Write(player.Position.Y);
                    reply.Write(player.Position.Z);
                }, others, NetDeliveryMethod.ReliableSequenced, 1);
            });

            NetWrapper.StartListening(14242, 128);

            while (NetWrapper.Status != NetPeerStatus.NotRunning) {
                if (!NetWrapper.CheckForMessages()) Thread.Sleep(16);

                var connected = NetWrapper.Connections;
                var disconnected = _players.Values.Where(x => !connected.Contains(x.Connection)).ToArray();

                if (disconnected.Length > 0) {
                    foreach (var player in disconnected) {
                        _players.Remove(player.Connection);
                    }

                    UpdatePlayers(connected);
                }
            }

            return 0;
        }
    }
}
