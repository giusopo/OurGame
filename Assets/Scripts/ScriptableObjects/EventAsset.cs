using UnityEngine;
using UnityEngine.Events;

namespace OurGame.Core
{
    /// <summary>
    /// A generic ScriptableObject-based event asset that can be raised from code or inspector.
    /// Useful for decoupling scene objects without code references.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/GameEvent", fileName = "NewGameEvent")]
    public class GameEvent : ScriptableObject
    {
        public UnityEvent response;

        public void Raise()
        {
            response?.Invoke();
        }
    }
}