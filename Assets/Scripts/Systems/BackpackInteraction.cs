using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OurGame.Systems
{
    [DisallowMultipleComponent]
    public class BackpackInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerPocketHoverHighlighter pocketHoverHighlighter;
        private bool hasLoggedMissingReference;

        public event Action<string> OnPocketClicked;

        void Reset()
        {
            AutoAssignReferences();
        }

        void OnValidate()
        {
            AutoAssignReferences();
        }

        void Awake()
        {
            AutoAssignReferences();
        }

        void Update()
        {
            if (pocketHoverHighlighter == null)
                AutoAssignReferences();

            if (!CanClickPocket())
                return;

            string hoveredPocket = pocketHoverHighlighter.CurrentHoveredPocketName;
            if (string.IsNullOrWhiteSpace(hoveredPocket))
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            OnPocketClicked?.Invoke(hoveredPocket);
        }

        private bool CanClickPocket()
        {
            if (pocketHoverHighlighter == null)
            {
                if (!hasLoggedMissingReference)
                {
                    Debug.LogWarning("BackpackInteraction has no PlayerPocketHoverHighlighter reference.");
                    hasLoggedMissingReference = true;
                }
                return false;
            }

            hasLoggedMissingReference = false;

            bool inventoryOpen = BackpackInventorySystem.Instance != null && BackpackInventorySystem.Instance.IsInventoryOpen;
            return Cursor.visible || Input.GetMouseButton(1) || inventoryOpen;
        }

        private void AutoAssignReferences()
        {
            if (pocketHoverHighlighter == null)
                pocketHoverHighlighter = GetComponentInParent<PlayerPocketHoverHighlighter>();

            if (pocketHoverHighlighter == null)
                pocketHoverHighlighter = FindFirstObjectByType<PlayerPocketHoverHighlighter>();

            if (pocketHoverHighlighter == null)
            {
                PlayerInteraction playerInteraction = FindFirstObjectByType<PlayerInteraction>();
                if (playerInteraction != null)
                {
                    pocketHoverHighlighter = playerInteraction.GetComponent<PlayerPocketHoverHighlighter>();
                    if (pocketHoverHighlighter == null)
                        pocketHoverHighlighter = playerInteraction.gameObject.AddComponent<PlayerPocketHoverHighlighter>();
                }
            }

            if (pocketHoverHighlighter == null)
            {
                PlayerPocketHoverHighlighter[] allHighlighters = Resources.FindObjectsOfTypeAll<PlayerPocketHoverHighlighter>();
                for (int i = 0; i < allHighlighters.Length; i++)
                {
                    PlayerPocketHoverHighlighter candidate = allHighlighters[i];
                    if (candidate != null && candidate.gameObject.scene.IsValid())
                    {
                        pocketHoverHighlighter = candidate;
                        break;
                    }
                }
            }

        }
    }
}
