using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    public string stageName;
    public long mapId;
    public Sprite thumbnail;
    public string data;
    public string portalPairs;
    public string rotationInfo;
}
