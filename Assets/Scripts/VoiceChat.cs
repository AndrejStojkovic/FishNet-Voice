using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    public bool Activated = true;
    public KeyCode PushToTalkKey;

    public AudioSource source;

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

        if(!previousCanTalk && canTalk)
        {
            Debug.Log("[VOICE] Talking!");
            source.clip = Microphone.Start(MicrophoneManager.Instance.GetCurrentDeviceName(), true, 1, AudioSettings.outputSampleRate);
            source.loop = true;
            source.Play();
        }

        if(previousCanTalk && !canTalk)
        {
            Debug.Log("[VOICE] End talking!");
            Microphone.End(MicrophoneManager.Instance.GetCurrentDeviceName());
            source.clip = null;
        }

        previousCanTalk = canTalk;
    }
}
