using UnityEngine;

[CreateAssetMenu(fileName = "NewNpcDialogue", menuName = "Npc Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcSprite;
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public float autoProgressDelay = 1.5f;
    public float typingSpeed = 0.05f;
    public AudioClip[] voiceSounds; //different clips per line
    public float voicePitch = 1f;

    //Mission speech
    [TextArea(2, 5)]
    public string[] missionLines;
    public AudioClip[] missionVoiceSounds;
}
