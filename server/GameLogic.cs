﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelLegacyGameServer
{
    class GameLogic
    {
        public static void Update()
        {
            foreach(Client _client in Server.clients.Values)
            {
                if(_client.player != null)
                {
                    _client.player.Update();
                }
            }

            ThreadManger.UpdateMain();
        }
    }
}
