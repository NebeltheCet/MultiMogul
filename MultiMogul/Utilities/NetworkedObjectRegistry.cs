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

        public static int GetGUIDHashFromInstance<T>(T objectInstance) where T : UnityEngine.Object
        {
            if (objectInstance == null)
            {
                throw new Exception("GetGUIDHashFromInstance called with null object instance!");
            }

            NetworkedObject networkedObject = objectInstance.GameObject().GetComponent<NetworkedObject>();
            if (networkedObject == null)
            {
                throw new Exception("GetGUIDHashFromInstance called on object without NetworkedObject component!");
            }

            return networkedObject.guidHash;
        }

        public static void Register<T>(T unityObject, string guid = "") where T : UnityEngine.Object
        {
            if (unityObject == null)
            {
                throw new Exception("AddNetworkComponent called with null GameObject!");
            }

            GameObject gameObject = unityObject.GameObject();

            if (gameObject.TryGetComponent<NetworkedObject>(out var component))
            {
                if (string.IsNullOrEmpty(guid) && string.IsNullOrEmpty(component.guid))
                {
                    component.guid = Guid.NewGuid().ToString();
                }
                else if (!string.IsNullOrEmpty(guid))
                {
                    component.guid = guid;
                }

                component.guidHash = component.guid.GetHashCode();
                return; // component already exists for this object
            }

            NetworkedObject networkedObject = gameObject.AddComponent<NetworkedObject>();
            networkedObject.guid = guid;
            if (string.IsNullOrEmpty(networkedObject.guid))
            {
                networkedObject.guid = Guid.NewGuid().ToString();
            }

            networkedObject.guidHash = networkedObject.guid.GetHashCode();

            networkedObjects.Add(gameObject.GameObject());
        }

        public static GameObject GetFromGUID(int guidHash)
        {
            if (networkedObjects.Count <= 0)
                return null;

            GameObject guidObject = null;
            foreach (var obj in networkedObjects)
            {
                if (obj == null)
                    continue;

                NetworkedObject networkedObject = obj.GetComponent<NetworkedObject>();
                if (networkedObject == null)
                    continue;

                if (networkedObject.guidHash != guidHash)
                    continue;

                guidObject = obj;
                break;
            }

            if (guidObject == null)
            {
                throw new Exception($"GetFromGUID could not find object with GUID hash {guidHash}!");
            }

            return guidObject;
        }

        public static void RemoveAllMatching(GameObject other)
        {
            networkedObjects.RemoveAll(x => x == other);
        }

        public static void Clear()
        {
            networkedObjects.Clear();
        }
    }
}
