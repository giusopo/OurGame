using UnityEngine;

namespace OurGame.Farming
{
    [CreateAssetMenu(menuName = "Farming/Crop Data", fileName = "NewCropData")]
    public class CropData : ScriptableObject
    {
        public string cropName;
        public Sprite icon;
        public float growTime; // seconds
        public int sellPrice;
        public GameObject prefab;
    }
}