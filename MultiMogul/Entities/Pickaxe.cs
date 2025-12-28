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

namespace MultiMogul.MultiMogul.Entities {
    public class Pickaxe {
        public static void SwingPickaxe(ToolPickaxe _instance) {
            FieldInfo _lastAttackTime = typeof(ToolPickaxe).GetField("_lastAttackTime", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _swingSoundPlayer = typeof(ToolPickaxe).GetField("_swingSoundPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _sound_swing = typeof(ToolPickaxe).GetField("_sound_swing", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_instance.Owner == null)
                return;

            if (_instance.Owner.GetComponentInChildren<Camera>() == null)
                return;

            if (_instance.ViewModelAnimator != null) {
                _instance.ViewModelAnimator.Play("Attack1", -1, 0f);
            }

            ((SoundPlayer)_swingSoundPlayer.GetValue(_instance)).PlaySound((SoundDefinition)_sound_swing.GetValue(_instance));
            _instance.StartCoroutine(PerformAttack(_instance, 0.2f));

            _lastAttackTime.SetValue(_instance, Time.time);
        }

        public static IEnumerator PerformAttack(ToolPickaxe _instance, float delaySeconds) {
            FieldInfo _sound_hit_node = typeof(ToolPickaxe).GetField("_sound_hit_node", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _sound_hit_world = typeof(ToolPickaxe).GetField("_sound_hit_world", BindingFlags.NonPublic | BindingFlags.Instance);

            yield return new WaitForSeconds(delaySeconds);
            Camera componentInChildren = _instance.Owner.GetComponentInChildren<Camera>();
            if (componentInChildren == null) {
                yield break;
            }

            int num = LayerMask.NameToLayer("BuildingPlacementCollider");
            int num2 = ~(1 << num);
            RaycastHit raycastHit;
            if (Physics.Raycast(componentInChildren.transform.position, componentInChildren.transform.forward, out raycastHit, _instance.UseRange, num2)) {
                IDamageable componentInParent = raycastHit.collider.GetComponentInParent<IDamageable>();
                if (componentInParent != null) {
                    componentInParent.TakeDamage(_instance.Damage, raycastHit.point);
                    Singleton<SoundManager>.Instance.PlaySoundAtLocation((SoundDefinition)_sound_hit_node.GetValue(_instance), raycastHit.point, 1f, 1f);
                    Singleton<ParticleManager>.Instance.CreateParticle(Singleton<ParticleManager>.Instance.OreNodeHitParticlePrefab, raycastHit.point, Quaternion.LookRotation(raycastHit.normal), default(Vector3));
                }
                else {
                    Singleton<SoundManager>.Instance.PlaySoundAtLocation((SoundDefinition)_sound_hit_world.GetValue(_instance), raycastHit.point, 1f, 1f);
                    Singleton<ParticleManager>.Instance.CreateParticle(Singleton<ParticleManager>.Instance.GenericHitImpactParticle, raycastHit.point, Quaternion.LookRotation(raycastHit.normal), default(Vector3));
                }

                Rigidbody component = raycastHit.collider.GetComponent<Rigidbody>();
                if (component != null) {
                    float num3 = 5f;
                    Vector3 forward = componentInChildren.transform.forward;
                    component.AddForceAtPosition(forward * num3, raycastHit.point, ForceMode.Impulse);

                    if (!ClientManager.IsHost())
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("SV_OnForceAdded");

                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(raycastHit.collider.transform.root.gameObject));
                            packet.Write(forward * num3);
                            packet.Write(raycastHit.point);

                            packet.Send(ClientManager.Instance.connection.Connection, Steamworks.Data.SendType.Reliable);
                        }
                    }
                    else
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_OnForceAdded");

                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(raycastHit.collider.transform.root.gameObject));
                            packet.Write(forward * num3);
                            packet.Write(raycastHit.point);

                            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                            {
                                packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                            }
                        }
                    }
                }
            }

            yield break;
        }

        [Networkable("SV_OnForceAdded")]
        public static void OnForceAddedServer(Connection connection, Packet receivedPacket)
        {
            int objectId = receivedPacket.ReadInt32();
            Vector3 force = receivedPacket.ReadVector3();
            Vector3 position = receivedPacket.ReadVector3();

            GameObject obj = NetworkedObjectRegistry.GetFromGUID(objectId);
            if (obj != null)
            {
                Rigidbody component = obj.GetComponent<Rigidbody>();

                component?.AddForceAtPosition(force, position, ForceMode.Impulse);
            }

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnForceAdded");

                packet.Write(objectId);
                packet.Write(force);
                packet.Write(position);

                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    if (kvp.Key == connection)
                        continue;

                    packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnForceAdded")]
        public static void OnForceAdded(Packet receivedPacket)
        {
            int objectId = receivedPacket.ReadInt32();
            Vector3 force = receivedPacket.ReadVector3();
            Vector3 position = receivedPacket.ReadVector3();

            GameObject obj = NetworkedObjectRegistry.GetFromGUID(objectId);
            if (obj != null)
            {
                Rigidbody component = obj.GetComponent<Rigidbody>();

                component?.AddForceAtPosition(force, position, ForceMode.Impulse);
            }

            receivedPacket.Dispose();
        }
    }
}
