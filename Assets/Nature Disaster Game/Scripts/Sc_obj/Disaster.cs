using UnityEngine;

[CreateAssetMenu(fileName = "New Disaster", menuName = "Disasters/New Disaster")]
public class Disaster : ScriptableObject
{
    public enum DisasterType { Flood, Earthquake, Hurricane, Volcano, AxidRain, BlackHole, Fire, Explosion, Tsunami}
    public DisasterType type;
    public string disasterName;
    public string Discription;
    public bool hasCloud = false;
    public bool hasFog = false;
}
