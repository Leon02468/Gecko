using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Phase")]
public class BossPhase : ScriptableObject
{
    public int enterAtHPPercent = 100;
    public BossAttack[] attacks;
    public float minDecisionGap = 0.6f;
    public float maxDecisionGap = 1.2f;
    public float moveSpeed = 4f;
    public float gravityScale = 3.5f;
}
