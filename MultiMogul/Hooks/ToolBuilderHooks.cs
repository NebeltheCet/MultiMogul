using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class ToolBuilderHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToolBuilder), "PrimaryFire")]
        public static bool PrePrimaryFire(ToolBuilder __instance)
        {
            if (__instance.Owner == null)
            {
                return false;
            }
            Camera componentInChildren = __instance.Owner.GetComponentInChildren<Camera>();
            if (componentInChildren == null)
            {
                return false;
            }

            MethodInfo GetBuildPosition = typeof(ToolBuilder).GetMethod("GetBuildPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _objectPrefab = typeof(ToolBuilder).GetField("_objectPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

            Vector3 buildPosition = (Vector3)GetBuildPosition.Invoke(__instance, new object[] { componentInChildren });
            Vector3Int closestGridPosition = Singleton<BuildingManager>.Instance.GetClosestGridPosition(buildPosition);
            BuildingPlacementNode buildingPlacementNode;
            if (Singleton<BuildingManager>.Instance.CanPlaceObject(closestGridPosition, (BuildingObject)_objectPrefab.GetValue(__instance), __instance.CurrentRotation, ((BuildingObject)_objectPrefab.GetValue(__instance)).RequiresFlatGround, ((BuildingObject)_objectPrefab.GetValue(__instance)).PlacementNodeRequirement, out buildingPlacementNode, __instance) == CanPlaceBuilding.Valid)
            {
                BuildingObject attachedBuildingObject = UnityEngine.Object.Instantiate<BuildingObject>((BuildingObject)_objectPrefab.GetValue(__instance), Singleton<BuildingManager>.Instance.GhostObjectTransform.position, Singleton<BuildingManager>.Instance.GhostObjectTransform.rotation);
                //NetworkedObjectRegistry.Register<GameObject>(attachedBuildingObject.gameObject);

                // NETWORKING
                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_OnBuildingPlaced");

                        packet.Write((int)attachedBuildingObject.GetSavableObjectID());
                        packet.Write(attachedBuildingObject.GetPosition());
                        packet.Write(attachedBuildingObject.GetRotation());
                        packet.Write(attachedBuildingObject.GetCustomSaveData());

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_OnBuildingPlaced");

                            packet.Write((int)attachedBuildingObject.GetSavableObjectID());
                            packet.Write(attachedBuildingObject.GetPosition());
                            packet.Write(attachedBuildingObject.GetRotation());
                            packet.Write(attachedBuildingObject.GetCustomSaveData());

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }
                // NETWORKING

                if (buildingPlacementNode != null)
                {
                    buildingPlacementNode.AttachBuilding(attachedBuildingObject);
                }
                if (!Singleton<DebugManager>.Instance.UnlimitedBuilding)
                {
                    __instance.Quantity--;
                    if (__instance.Quantity <= 0)
                    {
                        UnityEngine.Object.Destroy(__instance.gameObject);
                    }
                }
            }

            return false;
        }

        [Networkable("SV_OnBuildingPlaced")]
        public static void OnBuildingPlacedServer(Connection connection, Packet receivedPacket)
        {
            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;

            SavableObjectID savableObjectID = (SavableObjectID)receivedPacket.ReadInt32();
            Vector3 position = (Vector3)receivedPacket.ReadVector3();
            Vector3 rotation = (Vector3)receivedPacket.ReadVector3();
            string customDataJson = receivedPacket.ReadString();

            GameObject prefab = saveLoadManager.GetPrefab(savableObjectID);
            ISaveLoadableObject saveLoadableObject2;
            GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, position, Quaternion.Euler(rotation));
            //NetworkedObjectRegistry.Register<GameObject>(obj);
            if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
            {
                MinerHooks.allowOverride = true;
                saveLoadableObject2.LoadFromSave(customDataJson);
                MinerHooks.allowOverride = false;
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnBuildingPlaced");

                    packet.Write((int)savableObjectID);
                    packet.Write(position);
                    packet.Write(rotation);
                    packet.Write(customDataJson);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnBuildingPlaced")]    
        public static void OnBuildingPlaced(Packet receivedPacket)
        {
            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;

            SavableObjectID savableObjectID = (SavableObjectID)receivedPacket.ReadInt32();
            Vector3 position = (Vector3)receivedPacket.ReadVector3();
            Vector3 rotation = (Vector3)receivedPacket.ReadVector3();
            string customDataJson = receivedPacket.ReadString();

            GameObject prefab = saveLoadManager.GetPrefab(savableObjectID);
            ISaveLoadableObject saveLoadableObject2;
            GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, position, Quaternion.Euler(rotation));
            //NetworkedObjectRegistry.Register<GameObject>(obj);
            if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
            {
                MinerHooks.allowOverride = true;
                saveLoadableObject2.LoadFromSave(customDataJson);
                MinerHooks.allowOverride = false;
            }

            receivedPacket.Dispose();
        }
    }
}
