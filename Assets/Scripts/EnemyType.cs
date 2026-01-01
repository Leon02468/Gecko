using UnityEngine;

[CreateAssetMenu(menuName = "Game/EnemyType", fileName = "NewEnemyType")]
public class EnemyType : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite sprite;
}
