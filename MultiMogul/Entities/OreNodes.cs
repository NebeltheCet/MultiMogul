using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Entities
{
    public class OreNodes
    {
        public static OrePiece GetOrePrefab(OreNode oreNodeInstance, ref float value)
        {
            if (float.IsNaN(value))
            {
                value = UnityEngine.Random.value;
            }

            if (oreNodeInstance == null)
                return null;

            Type oreNodeType = oreNodeInstance.GetType();
            FieldInfo possibleDropsField = oreNodeType.GetField(
                "_possibleDrops",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (possibleDropsField == null)
                return null;

            IEnumerable dropsEnumerable = possibleDropsField.GetValue(oreNodeInstance) as IEnumerable;
            if (dropsEnumerable == null)
                return null;

            List<object> weightedDrops = [.. dropsEnumerable];
            if (weightedDrops.Count == 0)
                return null;

            Type weightedDropType = weightedDrops[0].GetType();
            FieldInfo weightField = weightedDropType.GetField(
                "Weight",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            FieldInfo prefabField = weightedDropType.GetField(
                "OrePrefab",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (weightField == null || prefabField == null)
                return null;

            float totalWeight = 0f;
            foreach (var drop in weightedDrops)
            {
                totalWeight += (float)weightField.GetValue(drop);
            }

            float roll = value * totalWeight;
            float accumulated = 0f;
            foreach (var drop in weightedDrops)
            {
                accumulated += (float)weightField.GetValue(drop);
                if (roll > accumulated)
                    continue;

                return (OrePiece)prefabField.GetValue(drop);
            }

            return (OrePiece)prefabField.GetValue(weightedDrops[weightedDrops.Count - 1]);
        }


        public static void BreakNode(OreNode _instance, Vector3 position, Packet receivedPacket = null)
        {
            Packet packet = new Packet(PacketType.OnRPCMessage);
            packet.Write("CL_OnBreakOreNode");
            packet.Write(_instance.transform.position);
            packet.Write(position);

            int num = UnityEngine.Random.Range(_instance.MinDrops, _instance.MaxDrops + 1);
            if (receivedPacket != null)
            {
                num = receivedPacket.ReadInt32();
            }
            else
            {
                packet.Write(num);
            }

            Vector3 vector = (_instance.transform.position + position) * 0.5f;
            for (int i = 0; i < num; i++)
            {
                Vector3 displacement = UnityEngine.Random.insideUnitSphere * 0.5f;
                if (receivedPacket != null)
                {
                    vector += receivedPacket.ReadVector3();
                }
                else
                {
                    vector += displacement;
                    packet.Write(displacement);
                }

                OrePiece orePrefab = null;
                if (receivedPacket == null)
                {
                    float oreRandomValue = float.NaN;
                    orePrefab = GetOrePrefab(_instance, ref oreRandomValue);

                    packet.Write(oreRandomValue);
                }
                else
                {
                    float oreRandomValue = receivedPacket.ReadSingle();

                    orePrefab = GetOrePrefab(_instance, ref oreRandomValue);
                }

                OrePiece orePiece = UnityEngine.Object.Instantiate<OrePiece>(orePrefab, vector, Quaternion.identity);
                if (orePiece != null)
                {
                    Rigidbody component = orePiece.GetComponent<Rigidbody>();
                    if (component != null)
                    {
                        component.linearVelocity = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(2f, 4f), UnityEngine.Random.Range(-1.5f, 1.5f));
                        component.angularVelocity = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(1f, 50f);
                        if (receivedPacket != null)
                        {
                            component.linearVelocity = receivedPacket.ReadVector3();
                            component.angularVelocity = receivedPacket.ReadVector3();
                        }
                        else
                        {
                            packet.Write(component.linearVelocity);
                            packet.Write(component.angularVelocity);
                        }
                    }

                    NetworkedObjectRegistry.Register<OrePiece>(orePiece);
                }
            }

            Singleton<SoundManager>.Instance.PlaySoundAtLocation(Singleton<SoundManager>.Instance.Sound_Ore_Crush, _instance.transform.position, 1f, 1f);
            Singleton<ParticleManager>.Instance.CreateParticle(Singleton<ParticleManager>.Instance.BreakOreNodeParticlePrefab, position, default(Quaternion), default(Vector3));
            _instance.UpdateSupportsAbove();
            _instance.MarkStaticPositionAsBroken();
            UnityEngine.Object.Destroy(_instance.gameObject);

            if (receivedPacket == null)
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            packet.Dispose();
        }

        [Networkable("CL_OnBreakOreNode")]
        public static void OnBreakOreNode(Packet receivedPacket)
        {
            Vector3 objectPosition = receivedPacket.ReadVector3();
            Vector3 damagePosition = receivedPacket.ReadVector3();

            GameObject nodeObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (nodeObject != null)
            {
                OreNode nodeComponent = nodeObject.GetComponent<OreNode>();
                if (nodeComponent != null)
                {
                    OreNodeHooks.allowOverride = true;
                    BreakNode(nodeComponent, damagePosition, receivedPacket);
                    OreNodeHooks.allowOverride = false;
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnOreNodeTakeDamage")]
        public static void OnOreNodeTakeDamageServer(Connection connection, Packet receivedPacket)
        {
            Vector3 objectPosition = receivedPacket.ReadVector3();
            float damageAmount = receivedPacket.ReadSingle();
            Vector3 damagePosition = receivedPacket.ReadVector3();

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreNodeTakeDamage");

                    packet.Write(objectPosition);
                    packet.Write(damageAmount);
                    packet.Write(damagePosition);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            GameObject nodeObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (nodeObject != null)
            {
                OreNode nodeComponent = nodeObject.GetComponent<OreNode>();
                if (nodeComponent != null)
                {
                    OreNodeHooks.allowOverride = true;
                    nodeComponent.TakeDamage(damageAmount, damagePosition);
                    OreNodeHooks.allowOverride = false;
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnOreNodeTakeDamage")]
        public static void OnOreNodeTakeDamage(Packet receivedPacket)
        {
            Vector3 objectPosition = receivedPacket.ReadVector3();
            float damageAmount = receivedPacket.ReadSingle();
            Vector3 damagePosition = receivedPacket.ReadVector3();

            GameObject nodeObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (nodeObject != null)
            {
                OreNode nodeComponent = nodeObject.GetComponent<OreNode>();
                if (nodeComponent != null)
                {
                    OreNodeHooks.allowOverride = true;
                    nodeComponent.TakeDamage(damageAmount, damagePosition);
                    OreNodeHooks.allowOverride = false;
                }
            }

            receivedPacket.Dispose();
        }
    }
}
