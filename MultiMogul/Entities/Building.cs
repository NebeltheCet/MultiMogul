using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiMogul.MultiMogul.Entities
{
    public class Building
    {
        public static void Pack(BuildingObject _instance, Connection? connection = null, Packet receivedPacket = null)
        {
            if (_instance.IsGhost)
                return;

            Packet packet = new Packet(PacketType.OnRPCMessage);
            if (ClientManager.IsHost())
            {
                packet.Write("CL_CrateObject");
            }
            else
            {
                packet.Write("SV_CrateObject");
            }

            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(_instance));

            Vector3 vector = (_instance.BuildingCrateSpawnPoint ? _instance.BuildingCrateSpawnPoint.position : (_instance.transform.position + new Vector3(0f, 0.25f, 0f)));
            Quaternion quaternion = (_instance.BuildingCrateSpawnPoint ? _instance.BuildingCrateSpawnPoint.rotation : Quaternion.identity);
            BuildingCrate buildingCrate = UnityEngine.Object.Instantiate<BuildingCrate>(_instance.Definition.PackedPrefab ? _instance.Definition.PackedPrefab : Singleton<BuildingManager>.Instance.BuildingCratePrefab, vector, quaternion);
            buildingCrate.Definition = _instance.Definition;

            if (receivedPacket == null)
            {
                NetworkedObjectRegistry.Register(buildingCrate);
                packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(buildingCrate));
            }
            else
            {
                NetworkedObjectRegistry.Register(buildingCrate, receivedPacket.ReadString());
                packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(buildingCrate));
            }

            Rigidbody component = buildingCrate.GetComponent<Rigidbody>();
            if (component != null)
            {
                if (receivedPacket == null)
                {
                    float linearRange = 0.5f;
                    Vector3 newLinearVelocity = new Vector3(UnityEngine.Random.Range(-linearRange, linearRange), UnityEngine.Random.Range(0f, linearRange) * 2f, UnityEngine.Random.Range(-linearRange, linearRange));
                    component.linearVelocity = newLinearVelocity;
                    packet.Write(component.linearVelocity);

                    float angularRange = 1f;
                    Vector3 newAngularVelocity = new Vector3(UnityEngine.Random.Range(-angularRange, angularRange), UnityEngine.Random.Range(-angularRange, angularRange), UnityEngine.Random.Range(-angularRange, angularRange));
                    component.angularVelocity = newAngularVelocity;
                    packet.Write(component.angularVelocity);
                }
                else
                {
                    component.linearVelocity = receivedPacket.ReadVector3();
                    component.angularVelocity = receivedPacket.ReadVector3();

                    packet.Write(component.linearVelocity);
                    packet.Write(component.angularVelocity);
                }
            }

            if (receivedPacket == null || ClientManager.IsHost())
            {
                if (ClientManager.IsHost())
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        if (connection.HasValue && kvp.Key == connection.Value)
                            continue;

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
                else
                {
                    if (!connection.HasValue)
                    {
                        throw new Exception("Pack was executed without connection for client.");
                    }

                    packet.Send(connection.Value, SendType.Reliable);
                }
            }

            UnityEngine.Object.Destroy(_instance.gameObject, 0f);
            packet.Dispose();
        }

        public static void UpdateGhostObject(BuildingManager _instance, Vector3Int position, BuildingObject prefab, Quaternion rotation, ToolBuilder activeTool)
        {
            FieldInfo _ghostObject = typeof(BuildingManager).GetField("_ghostObject", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _previousPosition = typeof(BuildingManager).GetField("_previousPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _isEligibleForSnapping = typeof(BuildingManager).GetField("_isEligibleForSnapping", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo SetupGhostObject = typeof(BuildingManager).GetMethod("SetupGhostObject", BindingFlags.NonPublic | BindingFlags.Instance);

            SetupGhostObject.Invoke(_instance, [position, prefab, rotation]);
            if (((Vector3)_previousPosition.GetValue(_instance)) != ((BuildingObject)_ghostObject.GetValue(_instance)).transform.position)
            {
                _isEligibleForSnapping.SetValue(_instance, true);
                _instance.CurrentObjectIsSnapped = false;

                _previousPosition.SetValue(_instance, ((BuildingObject)_ghostObject.GetValue(_instance)).transform.position);
            }

            Material material = _instance.GhostMaterial;
            CanPlaceBuilding canPlaceBuilding = _instance.CanPlaceObject(position, prefab, rotation, prefab.RequiresFlatGround, prefab.PlacementNodeRequirement, activeTool);
            if (canPlaceBuilding != CanPlaceBuilding.Invalid)
            {
                if (canPlaceBuilding == CanPlaceBuilding.RequirementsNotMet)
                {
                    material = _instance.RequirementGhostMaterial;
                }
            }
            else
            {
                material = _instance.InvalidGhostMaterial;
            }

            if (((BuildingObject)_ghostObject.GetValue(_instance)) == null)
            {
                SetupGhostObject.Invoke(_instance, [position, prefab, rotation]);
            }

            foreach (Renderer renderer in ((BuildingObject)_ghostObject.GetValue(_instance)).GetComponentsInChildren<Renderer>())
            {
                if (!(((BuildingObject)_ghostObject.GetValue(_instance)).ExtraGhostRenderers != null) || !renderer.transform.IsChildOf(((BuildingObject)_ghostObject.GetValue(_instance)).ExtraGhostRenderers.transform))
                {
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        if (!_instance.MaterialsToNotReplaceOnBuildingGhost.Contains(sharedMaterials[j]))
                        {
                            sharedMaterials[j] = material;
                        }
                    }

                    renderer.sharedMaterials = sharedMaterials;
                }
            }

            if ((Time.realtimeSinceStartup - BuildingManagerHooks.lastSentUpdateTime) > 0.1f)
            {
                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_OnUpdateGhostObject");
                        packet.Write(SteamClient.SteamId.Value);

                        packet.Write(position);
                        packet.Write(rotation);
                        packet.Write((int)canPlaceBuilding);

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_OnUpdateGhostObject");
                        packet.Write(SteamClient.SteamId.Value);

                        packet.Write(position);
                        packet.Write(rotation);
                        packet.Write((int)canPlaceBuilding);

                        foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                        {
                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }

                BuildingManagerHooks.lastSentUpdateTime = Time.realtimeSinceStartup;
            }
        }

        [Networkable("CL_OnUpdateGhostObject")]
        public static void OnUpdateGhostObject(Packet receivedPacket)
        {
            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnUpdateGhostObjectServer] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            if (player.ghostObject == null)
            {
                Debug.LogWarning("[OnUpdateGhostObjectServer] - invalid ghost object");
                receivedPacket.Dispose();
                return;
            }

            Vector3Int position = receivedPacket.ReadVector3Int();
            Quaternion rotation = receivedPacket.ReadQuaternion();
            CanPlaceBuilding canPlaceBuilding = (CanPlaceBuilding)receivedPacket.ReadInt32();

            Material material = BuildingManager.Instance.GhostMaterial;
            if (canPlaceBuilding != CanPlaceBuilding.Invalid)
            {
                if (canPlaceBuilding == CanPlaceBuilding.RequirementsNotMet)
                {
                    material = BuildingManager.Instance.RequirementGhostMaterial;
                }
            }
            else
            {
                material = BuildingManager.Instance.InvalidGhostMaterial;
            }

            foreach (Renderer renderer in player.ghostObject.GetComponentsInChildren<Renderer>())
            {
                if (!(player.ghostObject.ExtraGhostRenderers != null) || !renderer.transform.IsChildOf(player.ghostObject.ExtraGhostRenderers.transform))
                {
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        if (!BuildingManager.Instance.MaterialsToNotReplaceOnBuildingGhost.Contains(sharedMaterials[j]))
                        {
                            sharedMaterials[j] = material;
                        }
                    }

                    renderer.sharedMaterials = sharedMaterials;
                }
            }

            player.ghostObject.transform.position = position + new Vector3(0.5f, 0f, 0.5f);
            player.ghostObject.transform.rotation = rotation;

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnUpdateGhostObject")]
        public static void OnUpdateGhostObjectServer(Connection connection, Packet receivedPacket)
        {
            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnUpdateGhostObjectServer] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            if (player.ghostObject == null)
            {
                Debug.LogWarning("[OnUpdateGhostObjectServer] - invalid ghost object");
                receivedPacket.Dispose();
                return;
            }

            Vector3Int position = receivedPacket.ReadVector3Int();
            Quaternion rotation = receivedPacket.ReadQuaternion();
            CanPlaceBuilding canPlaceBuilding = (CanPlaceBuilding)receivedPacket.ReadInt32();

            Material material = BuildingManager.Instance.GhostMaterial;
            if (canPlaceBuilding != CanPlaceBuilding.Invalid)
            {
                if (canPlaceBuilding == CanPlaceBuilding.RequirementsNotMet)
                {
                    material = BuildingManager.Instance.RequirementGhostMaterial;
                }
            }
            else
            {
                material = BuildingManager.Instance.InvalidGhostMaterial;
            }

            foreach (Renderer renderer in player.ghostObject.GetComponentsInChildren<Renderer>())
            {
                if (!(player.ghostObject.ExtraGhostRenderers != null) || !renderer.transform.IsChildOf(player.ghostObject.ExtraGhostRenderers.transform))
                {
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        if (!BuildingManager.Instance.MaterialsToNotReplaceOnBuildingGhost.Contains(sharedMaterials[j]))
                        {
                            sharedMaterials[j] = material;
                        }
                    }

                    renderer.sharedMaterials = sharedMaterials;
                }
            }

            player.ghostObject.transform.position = position + new Vector3(0.5f, 0f, 0.5f);
            player.ghostObject.transform.rotation = rotation;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnUpdateGhostObject");
                packet.Write(steamId);

                packet.Write(position);
                packet.Write(rotation);
                packet.Write((int)canPlaceBuilding);

                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    if (kvp.Key == connection)
                        continue;

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnSetupGhostObject")]
        public static void OnSetupGhostObject(Packet receivedPacket)
        {
            MethodInfo SetLayerRecursively = typeof(BuildingManager).GetMethod("SetLayerRecursively", BindingFlags.NonPublic | BindingFlags.Instance);

            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnSetupGhostObject] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            Vector3Int position = receivedPacket.ReadVector3Int();
            SavableObjectID prefabId = (SavableObjectID)receivedPacket.ReadInt32();
            Quaternion rotation = receivedPacket.ReadQuaternion();

            if (player.ghostObject == null)
            {
                GameObject prefabObject = SavingLoadingManager.Instance.GetPrefab(prefabId);
                BuildingObject prefab = prefabObject.GetComponent<BuildingObject>();

                player.ghostObject = UnityEngine.Object.Instantiate<BuildingObject>(prefab);
                player.ghostObject.IsGhost = true;
                foreach (Collider collider in player.ghostObject.GetComponentsInChildren<Collider>())
                {
                    if (collider.isTrigger)
                    {
                        UnityEngine.Object.Destroy(collider.gameObject);
                    }
                }

                foreach (MonoBehaviour monoBehaviour in player.ghostObject.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!(monoBehaviour is BuildingObject))
                    {
                        monoBehaviour.enabled = false;
                    }
                }

                AudioSource[] componentsInChildren3 = player.ghostObject.GetComponentsInChildren<AudioSource>(true);
                for (int i = 0; i < componentsInChildren3.Length; i++)
                {
                    componentsInChildren3[i].enabled = false;
                }

                int num = LayerMask.NameToLayer("BuildingGhost");
                SetLayerRecursively.Invoke(BuildingManager.Instance, [player.ghostObject.gameObject, num]);

                Rigidbody[] componentsInChildren4 = player.ghostObject.GetComponentsInChildren<Rigidbody>();
                for (int i = 0; i < componentsInChildren4.Length; i++)
                {
                    componentsInChildren4[i].isKinematic = true;
                }

                ParticleSystem[] componentsInChildren5 = player.ghostObject.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < componentsInChildren5.Length; i++)
                {
                    componentsInChildren5[i].enableEmission = false;
                }
            }

            player.ghostObject.transform.position = position + new Vector3(0.5f, 0f, 0.5f);
            player.ghostObject.transform.rotation = rotation;
        }

        [Networkable("SV_OnSetupGhostObject")]
        public static void OnSetupGhostObjectServer(Connection connection, Packet receivedPacket)
        {
            MethodInfo SetLayerRecursively = typeof(BuildingManager).GetMethod("SetLayerRecursively", BindingFlags.NonPublic | BindingFlags.Instance);

            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnSetupGhostObjectServer] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            Vector3Int position = receivedPacket.ReadVector3Int();
            SavableObjectID prefabId = (SavableObjectID)receivedPacket.ReadInt32();
            Quaternion rotation = receivedPacket.ReadQuaternion();

            if (player.ghostObject == null)
            {
                GameObject prefabObject = SavingLoadingManager.Instance.GetPrefab(prefabId);
                BuildingObject prefab = prefabObject.GetComponent<BuildingObject>();

                player.ghostObject = UnityEngine.Object.Instantiate<BuildingObject>(prefab);
                player.ghostObject.IsGhost = true;
                foreach (Collider collider in player.ghostObject.GetComponentsInChildren<Collider>())
                {
                    if (collider.isTrigger)
                    {
                        UnityEngine.Object.Destroy(collider.gameObject);
                    }
                }

                foreach (MonoBehaviour monoBehaviour in player.ghostObject.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!(monoBehaviour is BuildingObject))
                    {
                        monoBehaviour.enabled = false;
                    }
                }

                AudioSource[] componentsInChildren3 = player.ghostObject.GetComponentsInChildren<AudioSource>(true);
                for (int i = 0; i < componentsInChildren3.Length; i++)
                {
                    componentsInChildren3[i].enabled = false;
                }

                int num = LayerMask.NameToLayer("BuildingGhost");
                SetLayerRecursively.Invoke(BuildingManager.Instance, [player.ghostObject.gameObject, num]);

                Rigidbody[] componentsInChildren4 = player.ghostObject.GetComponentsInChildren<Rigidbody>();
                for (int i = 0; i < componentsInChildren4.Length; i++)
                {
                    componentsInChildren4[i].isKinematic = true;
                }

                ParticleSystem[] componentsInChildren5 = player.ghostObject.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < componentsInChildren5.Length; i++)
                {
                    componentsInChildren5[i].enableEmission = false;
                }
            }

            player.ghostObject.transform.position = position + new Vector3(0.5f, 0f, 0.5f);
            player.ghostObject.transform.rotation = rotation;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnSetupGhostObject");
                packet.Write(steamId);

                packet.Write(position);
                packet.Write((int)prefabId);
                packet.Write(rotation);

                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    if (kvp.Key == connection)
                        continue;

                    packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnCleanUpGhostObject")]
        public static void OnCleanUpGhostObject(Packet receivedPacket)
        {
            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnCleanUpGhostObject] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            if (player.ghostObject != null)
            {
                UnityEngine.Object.Destroy(player.ghostObject.gameObject);
                player.ghostObject = null;
            }

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnCleanUpGhostObject")]
        public static void OnCleanUpGhostObjectServer(Connection connection, Packet receivedPacket)
        {
            ulong steamId = receivedPacket.ReadUInt64();

            Player player = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (player == null)
            {
                Debug.LogWarning("[OnCleanUpGhostObjectServer] - failed to find player by steam id");
                receivedPacket.Dispose();
                return;
            }

            if (player.ghostObject != null)
            {
                UnityEngine.Object.Destroy(player.ghostObject.gameObject);
                player.ghostObject = null;
            }

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnCleanUpGhostObject");
                packet.Write(steamId);

                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    if (kvp.Key == connection)
                        continue;

                    packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }
    }
}
