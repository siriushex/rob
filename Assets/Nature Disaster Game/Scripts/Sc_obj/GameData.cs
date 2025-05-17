using UnityEngine;


[System.Serializable]
public class ExplosionSettings
{
    public GameObject explosionVFX;     // Префаб частиц взрыва
    public float explosionRadius = 5f;  // Радиус действия волны
    public Vector2 delayRange = new Vector2(1f, 2f); // Интервал между взрывами
    public int explosionsCount = 25;    // Сколько взрывов произойдёт за бедствие
}
[System.Serializable]
public class MapsData
{
    public string name;
    public Sprite icon;
    public GameObject map;

}
[CreateAssetMenu(fileName = "New GameData", menuName = "Disasters/GameData")]
public class GameData : ScriptableObject
{
    public Disaster[] disasters;

    [Header("Game Settings")]
    public int nextRound_startTime;
    public int roundTime;
    public int disasterWaitTime = 10;

    [Header("Maps")]
    public MapsData[] maps;

    [Header("MainPlayerParametr")]
    public PlayerParametrs player;

    [Header("Bot")]
    public string[] player_Names;
    public Sprite[] player_Icons;

    public int maxBot = 15;
    public int minBot = 5;

    [Header("Axid Rain")]
    public Material[] stateMaterials;
    public int changeStateInterval = 3;

    [Header("ExplosionSetting")]
    public ExplosionSettings explosionSettings;
}
