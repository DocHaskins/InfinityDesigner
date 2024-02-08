using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using UnityEngine.UI;

namespace doppelganger
{
    public class ObjectPool : MonoBehaviour
    {
        public TextureLoader loader;

        public static ObjectPool SharedInstance;
        public List<GameObject> pooledObjects;
        public GameObject objectToPool;
        public int amountToPool;
        public float ItemHeight { get; private set; }

        void Awake()
        {
            SharedInstance = this;
            loader = GameObject.Find("Manager").GetComponent<TextureLoader>();
            pooledObjects = new List<GameObject>();

            for (int i = 0; i < amountToPool; i++)
            {
                GameObject obj = Instantiate(objectToPool);
                obj.SetActive(true); // Temporarily activate to setup RectTransform
                LayoutRebuilder.ForceRebuildLayoutImmediate(obj.GetComponent<RectTransform>());
                obj.SetActive(false); // Then deactivate
                pooledObjects.Add(obj);
            }

            if (pooledObjects.Count > 0)
            {
                GameObject sampleButton = pooledObjects[0]; // Use the first instantiated object
                sampleButton.SetActive(true); // Temporarily activate to get the correct size
                ItemHeight = sampleButton.GetComponent<RectTransform>().sizeDelta.y;
                sampleButton.SetActive(false); // Deactivate again
                Debug.Log($"ObjectPool: Item height measured as: {ItemHeight}");
            }
            else
            {
                Debug.LogError("ObjectPool: No objects were instantiated for the pool.");
            }
        }

        public GameObject GetPooledObject()
        {
            for (int i = 0; i < amountToPool; i++)
            {
                if (!pooledObjects[i].activeInHierarchy)
                {
                    return pooledObjects[i];
                }
            }
            return null;
        }
    }
}