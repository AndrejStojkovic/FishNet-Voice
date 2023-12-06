using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    public bool Activated = true;
    public KeyCode PushToTalkKey;

    private bool previousCanTalk = false;
    private bool canTalk = true;

    void Start()
    {
        canTalk = true;
        previousCanTalk = canTalk;
    }

    void Update()
    {
        if(!Activated)
        {
            return;
        }

        canTalk = Input.GetKey(PushToTalkKey);

        if(canTalk)
        {
            // TO-DO: Record audio, stream audio to all players
            Debug.Log("[VOICE] Talking!");
        }

        if(previousCanTalk && !canTalk)
        {
            // TO-DO: If player stops talking, stop recording (only for push-to-talk)
            Debug.Log("[VOICE] End talking!");
        }

        previousCanTalk = canTalk;
    }
}
