using UnityEngine;

public class ChaseTrigger : MonoBehaviour
{
    public FlyingEnemy[] enemyArray;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        foreach(FlyingEnemy enemy in enemyArray)
        {

        }
    }
}
