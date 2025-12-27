using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class BuildingObjectHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingObject), "Pack")]
        public static bool PrePack(BuildingObject __instance)
        {
            if (__instance.IsGhost)
            {
                return false;
            }
            Vector3 position = __instance.BuildingCrateSpawnPoint ? __instance.BuildingCrateSpawnPoint.position : (__instance.transform.position + new Vector3(0f, 0.25f, 0f));
            Quaternion rotation = __instance.BuildingCrateSpawnPoint ? __instance.BuildingCrateSpawnPoint.rotation : Quaternion.identity;
            BuildingCrate buildingCrate = UnityEngine.Object.Instantiate<BuildingCrate>(__instance.Definition.PackedPrefab ? __instance.Definition.PackedPrefab : Singleton<BuildingManager>.Instance.BuildingCratePrefab, position, rotation);
            buildingCrate.Definition = __instance.Definition;
            Rigidbody component = buildingCrate.GetComponent<Rigidbody>();
            if (component != null)
            {
                float num = 0.5f;
                Vector3 linearVelocity = new Vector3(UnityEngine.Random.Range(-num, num), UnityEngine.Random.Range(0f, num) * 2f, UnityEngine.Random.Range(-num, num));
                component.linearVelocity = linearVelocity;
                float num2 = 1f;
                Vector3 angularVelocity = new Vector3(UnityEngine.Random.Range(-num2, num2), UnityEngine.Random.Range(-num2, num2), UnityEngine.Random.Range(-num2, num2));
                component.angularVelocity = angularVelocity;
            }
            UnityEngine.Object.Destroy(__instance.gameObject, 0f);
            //network destroy

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingObject), "TryAddToInventory")]
        public static bool PreTryAddToInventory(BuildingObject __instance, ref bool __result)
        {
            if (__instance.Definition == null)
            {
                Debug.LogWarning("Tried to pickup Crate with missing Building Definition!");
                __result = false;
                return false;
            }

            ToolBuilder toolBuilder = UnityEngine.Object.Instantiate<ToolBuilder>(Singleton<BuildingManager>.Instance.BuildingToolPrefab);
            toolBuilder.Definition = __instance.Definition;
            toolBuilder.Setup();

            if (UnityEngine.Object.FindObjectOfType<PlayerInventory>().TryAddToInventory(toolBuilder, -1))
            {
                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_DestroyObject");
                        packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance));

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_DestroyObject");
                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance));

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }

                // network destroy
                UnityEngine.Object.Destroy(__instance.gameObject, 0f);
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }


        [Networkable("SV_DestroyObject")]
        public static void DestroyObjectServer(Connection connection, Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();

            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);
            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_DestroyObject");
                    packet.Write(guidHash);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            UnityEngine.Object.Destroy(gameObject, 0f);

            receivedPacket.Dispose();
        }

        [Networkable("CL_DestroyObject")]
        public static void DestroyObject(Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();

            Debug.Log("Received request to destroy object with GUID Hash: " + guidHash);

            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);
            UnityEngine.Object.Destroy(gameObject, 0f);

            receivedPacket.Dispose();
        }
    }
}
