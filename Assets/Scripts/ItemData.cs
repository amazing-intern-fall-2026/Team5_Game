using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Farm Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
}