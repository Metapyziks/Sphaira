using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Sphaira.Server.Network;

namespace Sphaira.Server
{
    public class Program
    {
        public static int Main(String[] args)
        {
            var rand = new Random();

            int skySeed = rand.Next(1, int.MaxValue);

            NetWrapper.RegisterMessageHandler("WorldInfo", msg => {
                var reply = NetWrapper.CreateMessage("WorldInfo");

                reply.Write(skySeed);

                NetWrapper.SendMessage(reply, msg.SenderConnection, NetDeliveryMethod.ReliableUnordered);
            });

            NetWrapper.StartListening(14242, 128);

            while (NetWrapper.Status != NetPeerStatus.NotRunning) {
                NetWrapper.CheckForMessages();
                Thread.Sleep(16);
            }

            return 0;
        }
    }
}
