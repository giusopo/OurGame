using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Systems
{
    [DisallowMultipleComponent]
    public class BackpackDefinition : MonoBehaviour
    {
        private const string HologramObjectName = "AstronautBackpackHologram";

        [SerializeField] private Transform hologramRoot;
        [SerializeField] private List<BackpackPocketDefinition> pocketDefinitions = new List<BackpackPocketDefinition>();

        public Transform HologramRoot => hologramRoot;
        public IReadOnlyList<BackpackPocketDefinition> PocketDefinitions => pocketDefinitions;

        void Reset()
        {
            AutoAssignReferences();
            EnsureDefinitions();
        }

        void OnValidate()
        {
            AutoAssignReferences();
            EnsureDefinitions();
        }

        public List<BackpackPocketDefinition> GetDefinitionsSnapshot()
        {
            EnsureDefinitions();

            List<BackpackPocketDefinition> snapshot = new List<BackpackPocketDefinition>();
            HashSet<string> seen = new HashSet<string>();

            for (int i = 0; i < pocketDefinitions.Count; i++)
            {
                BackpackPocketDefinition definition = pocketDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.PocketName))
                    continue;

                definition.Normalize();
                if (!seen.Add(definition.PocketName))
                    continue;

                snapshot.Add(definition.Clone());
            }

            return snapshot;
        }

        private void AutoAssignReferences()
        {
            if (hologramRoot == null)
                hologramRoot = FindDescendantByName(transform, HologramObjectName);
        }

        private void EnsureDefinitions()
        {
            if (pocketDefinitions == null)
                pocketDefinitions = new List<BackpackPocketDefinition>();

            if (pocketDefinitions.Count == 0)
                BuildDefinitionsFromBackpackVisuals();

            HashSet<string> seen = new HashSet<string>();
            for (int i = pocketDefinitions.Count - 1; i >= 0; i--)
            {
                BackpackPocketDefinition definition = pocketDefinitions[i];
                if (definition == null)
                    continue;

                if (string.IsNullOrWhiteSpace(definition.PocketName))
                    continue;

                definition.Normalize();
                if (!seen.Add(definition.PocketName))
                    pocketDefinitions.RemoveAt(i);
            }
        }

        private void BuildDefinitionsFromBackpackVisuals()
        {
            Transform searchRoot = hologramRoot != null ? hologramRoot : transform;
            if (searchRoot == null)
                return;

            for (int i = 0; i < PocketNames.Ordered.Length; i++)
            {
                string pocketName = PocketNames.Ordered[i];
                if (FindDescendantByName(searchRoot, pocketName) == null)
                    continue;

                pocketDefinitions.Add(BackpackPocketDefinition.CreateDefault(pocketName));
            }
        }

        private static Transform FindDescendantByName(Transform root, string objectName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == objectName)
                    return child;

                Transform nested = FindDescendantByName(child, objectName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
