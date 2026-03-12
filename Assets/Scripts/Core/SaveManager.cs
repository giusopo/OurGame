using System.IO;
using UnityEngine;

namespace OurGame.Core
{
    [System.Serializable]
    public class SaveData
    {
        public int day;
        // add other serializable state here
    }

    public class SaveManager : SingletonMono<SaveManager>
    {
        private string saveFile => Path.Combine(Application.persistentDataPath, "save.json");

        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                day = GameManager.Instance.currentDay
            };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFile, json);
        }

        public void LoadGame()
        {
            if (File.Exists(saveFile))
            {
                string json = File.ReadAllText(saveFile);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                GameManager.Instance.currentDay = data.day;
            }
        }
    }
}