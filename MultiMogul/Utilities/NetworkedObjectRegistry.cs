using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities
{
    public class NetworkedObjectRegistry
    {
        private static List<GameObject> networkedObjects = new List<GameObject>();

        public static void Register<T>(T gameObject) where T : UnityEngine.Object
        {
            if (gameObject == null)
            {
                Debug.LogWarning("AddNetworkComponent called with null GameObject!");
                return;
            }

            NetworkedObject networkedObject = gameObject.AddComponent<NetworkedObject>();
            networkedObject.networkID = networkedObjects.Count;

            networkedObjects.Add(gameObject.GameObject());
        }

        // this is awful :c
        public static GameObject GetFromPosition(Vector3 position)
        {
            if (networkedObjects.Count <= 0)
                return null;

            float closestDistance = float.MaxValue;
            GameObject closestObject = null;
            foreach (var obj in networkedObjects)
            {
                if (obj == null)
                    continue;

                if (obj.transform == null)
                    continue;

                float distance = Vector3.Distance(obj.transform.position, position);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestObject = obj;
            }

            return closestObject;
        }

        public static void Clear()
        {
            networkedObjects.Clear();
        }
    }
}
