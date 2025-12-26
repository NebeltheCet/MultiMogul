using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class SellerMachineHooks
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SellerMachine), "DelayThenSellOre")]
        public static bool PreDelayThenSellOre(SellerMachine __instance, OrePiece ore)
        {
            return false;
        }

        public static IEnumerator DelayThenSellOre(SellerMachine __instance, OrePiece ore)
        {
            ore.gameObject.tag = "MarkedForDestruction";
            Transform[] componentsInChildren = ore.transform.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.tag = "MarkedForDestruction";
            }
            yield return new WaitForSeconds(2f);
            if (ore == null)
            {
                yield break;
            }

            if(ClientManager.IsHost())
            {
                float sellValue = ore.GetSellValue();
                Singleton<EconomyManager>.Instance.AddMoney(sellValue);
                QuestManager instance = Singleton<QuestManager>.Instance;
                if (instance != null)
                {
                    instance.OnResourceDeposited(ore.ResourceType, ore.PieceType, ore.PolishedPercent, 1);
                }
            }

            ore.Delete();
            yield break;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SellerMachine), "OnTriggerEnter")]
        public static bool PreOnTriggerEnter(SellerMachine __instance, UnityEngine.Collider other)
        {
            if (other.CompareTag("MarkedForDestruction"))
            {
                return false;
            }
            Rigidbody attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody == null)
            {
                return false;
            }
            OrePiece componentInParent = other.GetComponentInParent<OrePiece>();
            if (componentInParent != null)
            {
                __instance.StartCoroutine(DelayThenSellOre(__instance, componentInParent));
                if (componentInParent != null && componentInParent.CurrentMagnetTool != null)
                {
                    componentInParent.CurrentMagnetTool.DetachBody(attachedRigidbody);
                    return false;
                }
            }
            else
            {
                BaseSellableItem componentInParent2 = other.GetComponentInParent<BaseSellableItem>();
                if (componentInParent2 != null)
                {
                    Singleton<EconomyManager>.Instance.AddMoney(componentInParent2.GetSellValue());
                    UnityEngine.Object.Destroy(componentInParent2.gameObject);
                }
            }

            return false;
        }
    }
}
