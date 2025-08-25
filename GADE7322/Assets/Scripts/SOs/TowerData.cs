using UnityEngine;

[CreateAssetMenu(fileName = "TowerDefense", menuName = "Towers/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject towerPrefab;  
    public GameObject ghostPrefab; 

    [Header("UI Info")]
    public string towerName;
    [TextArea] public string description;
    public Sprite icon;
    public int cost;
    
}
