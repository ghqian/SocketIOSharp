﻿using Newtonsoft.Json.Linq;
using SocketIOSharp.Client;
using System;
using System.Collections.Generic;

namespace SocketIOSharp.Packet.Binary.Constructors
{
    internal class Deconstructor : Constructor
    {
        private int PlaceholderCount = 0;

        public override void SetPacket(SocketIOPacket ConstructeePacket)
        {
            if (ConstructeePacket != null)
            {
                base.SetPacket(ConstructeePacket);

                ConstructorAction EnqueueAction = new ConstructorAction((Data) => ConstructeeTokens.Enqueue(Data));
                ConstructorAction CountAction = new ConstructorAction((Data) => PlaceholderCount++);

                Tuple<Condition, ConstructorAction>[] ConditionalActions = new Tuple<Condition, ConstructorAction>[]
                {
                    new Tuple<Condition, ConstructorAction>(IsBytes, EnqueueAction),
                    new Tuple<Condition, ConstructorAction>(IsPlaceholder, CountAction)
                };

                DFS(ConstructeePacket.JsonData, ConditionalActions);

                if (PlaceholderCount > 0)
                {
                    throw new SocketIOClientException("Bytes token count is not match to placeholder count. " + this);
                }
            }
        }

        public SocketIOPacket Deconstruct()
        {
            int PlaceholderIndex = 0;

            while (ConstructeeTokenCount > 0)
            {
                JToken Parent = DequeueConstructeeTokenParent(out object Key);
                ConstructeePacket.Attachments.Enqueue(SocketIOPacket.Decode(EngineIOPacketType.MESSAGE, (byte[])Parent[Key]));

                Parent[Key] = new JObject
                {
                    [PLACEHOLDER] = true,
                    [NUM] = PlaceholderIndex++
                };
            }

            return ConstructeePacket;
        }

        public override string ToString()
        {
            return string.Format("Deconstructor: {0}, PlaceholderCount={1}", base.ToString(), PlaceholderCount);
        }
    }
}
