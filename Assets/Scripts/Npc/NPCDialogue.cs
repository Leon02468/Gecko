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
    public AudioClip voiceSound;
    public float voicePitch = 1f;
}
