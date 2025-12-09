using UnityEngine;
using System.Collections;

public class IntroController : MonoBehaviour
{
    public float introDuration = 5f;

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        // TODO: replace with Timeline, animations, dialogues
        yield return new WaitForSeconds(introDuration);

        // when intro ends:
        if (GameManager.Instance != null) GameManager.Instance.FinishIntroAndStartGameplay();
    }
}
