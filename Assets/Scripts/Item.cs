using UnityEngine.Tilemaps;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Item")]

public class Item : ScriptableObject{
    
    [Header("Only gameplay")]
    public TileBase title;
    public ItemType type;
    public ActionType actionType;
    public Vector2Int range = new Vector2Int(5,4);

    [Header("Only UI")]
     public bool stackable = true;


    [Header("Both")]
     public Sprite image;
}


public enum ItemType{
    BuildingBlock,
    Tool
}

public enum ActionType{
    Dig,
    Mine
}