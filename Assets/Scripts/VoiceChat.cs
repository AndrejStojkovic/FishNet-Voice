using System.Collections;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class VoiceChat : NetworkBehaviour
{
    public enum ChatType { Global, Proximity }
    public ChatType VoiceChatType = ChatType.Global;

    public enum DetectionType { PushToTalk, VoiceActivation }
    public DetectionType VoiceDetectionType = DetectionType.PushToTalk;

    public bool Activated = true;
    public KeyCode PushToTalkKey;

    public AudioSource source;
    public float proximityRange = 10f;
    public float voiceActivationThreshold = 0.002f;

    private bool canTalk = true;
    private bool previousCanTalk = false;

    private string deviceName;
    private const int sampleRate = 48000;
    private const int bufferSize = 16384;

    private float[] audioBuffer;
    private int position;

    private AudioClip microphoneClip;

    private float[] sampleData;
    private float[] micDataBuffer;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            return;

        if (source == null)
            Debug.LogError("[VOICE] AudioSource not assigned!");

        deviceName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

        if (string.IsNullOrEmpty(deviceName))
            Debug.LogError("[VOICE] No microphone device found!");

        audioBuffer = new float[bufferSize];
        sampleData = new float[bufferSize];
        micDataBuffer = new float[bufferSize];
        source.playOnAwake = false;

        micOutputSlider = FindObjectOfType<MicOutput>();
    }

    void Update()
    {
        if (!Activated || !IsOwner)
            return;

        string selectedDevice = MicrophoneManager.Instance.GetCurrentDeviceName();
        if (selectedDevice != deviceName)
        {
            UpdateMicrophone(selectedDevice);
        }

        switch (VoiceDetectionType)
        {
            case DetectionType.PushToTalk:
                canTalk = Input.GetKey(PushToTalkKey);
                if (canTalk && microphoneClip == null)
                {
                    StartMicrophone();
                    StartTalking();
                }
                else if (!canTalk && microphoneClip != null)
                {
                    StopTalking();
                    StopMicrophone();
                }
                break;

            case DetectionType.VoiceActivation:
                if (microphoneClip == null)
                {
                    StartMicrophone();
                }
                canTalk = IsVoiceActivated();
                break;
        }

        if (!previousCanTalk && canTalk)
            StartTalking();

        if (previousCanTalk && !canTalk)
            StopTalking();

        previousCanTalk = canTalk;
    }

    private void StartMicrophone()
    {
        if (string.IsNullOrEmpty(deviceName))
            return;

        position = 0;
        microphoneClip = Microphone.Start(deviceName, true, 10, sampleRate);
    }

    private void StopMicrophone()
    {
        if (string.IsNullOrEmpty(deviceName))
            return;

        Microphone.End(deviceName);
        microphoneClip = null;
    }

    private void UpdateMicrophone(string newDeviceName)
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            Debug.Log($"[VOICE] Switching microphone from '{deviceName}' to '{newDeviceName}'");

            // Stop the current microphone
            StopTalking();
            StopMicrophone();
        }

        deviceName = newDeviceName;

        // If currently talking, restart with the new microphone
        if (canTalk)
        {
            StartMicrophone();
            StartTalking();
        }
    }

    private void StartTalking()
    {
        if (string.IsNullOrEmpty(deviceName))
            return;

        StartCoroutine(TransmitVoice());
    }

    private void StopTalking()
    {
        if (string.IsNullOrEmpty(deviceName))
            return;

        StopCoroutine(TransmitVoice());
    }

    private IEnumerator TransmitVoice()
    {
        while (canTalk)
        {
            if (microphoneClip == null)
                yield break;

            int micPosition = Microphone.GetPosition(deviceName);

            if (micPosition < position)
                position = micPosition;

            if (position + bufferSize > micPosition)
            {
                yield return null;
                continue;
            }

            microphoneClip.GetData(audioBuffer, position);
            position = (position + bufferSize) % microphoneClip.samples;

            TransmitAudioServerRpc(audioBuffer);

            yield return new WaitForSeconds(bufferSize / (float)sampleRate);
        }
    }

    private bool IsVoiceActivated()
    {
        if (microphoneClip == null)
            return false;

        int micPosition = Microphone.GetPosition(deviceName);

        int sampleStartPosition = micPosition - bufferSize;
        if (sampleStartPosition < 0)
        {
            // Not enough data yet
            return false;
        }

        microphoneClip.GetData(sampleData, sampleStartPosition);

        float sum = 0;
        for (int i = 0; i < sampleData.Length; i++)
        {
            sum += Mathf.Abs(sampleData[i]);
        }

        float average = sum / sampleData.Length;
        return average > voiceActivationThreshold;
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransmitAudioServerRpc(float[] audioData, NetworkConnection sender = null)
    {
        TransmitAudioObserversRpc(audioData, sender.ClientId);
    }

    [ObserversRpc]
    private void TransmitAudioObserversRpc(float[] audioData, int senderClientId)
    {
        // Ensure we do not play our own voice
        if (senderClientId == NetworkManager.ClientManager.Connection.ClientId)
            return;

        PlayReceivedAudio(audioData, senderClientId);
    }

    private void PlayReceivedAudio(float[] audioData, int senderClientId)
    {
        if (source == null)
        {
            Debug.LogError("[VOICE] AudioSource not assigned!");
            return;
        }

        // Set spatial blend based on chat type
        if (VoiceChatType == ChatType.Proximity)
        {
            source.spatialBlend = 1.0f; // Make the audio 3D
            source.maxDistance = proximityRange;
            Transform senderTransform = GetPlayerTransform(senderClientId);
            if (senderTransform != null)
            {
                float distance = Vector3.Distance(transform.position, senderTransform.position);
                if (distance > proximityRange)
                {
                    return; // This is to save on bandwidth
                }
            }
        }
        else
        {
            source.spatialBlend = 0.0f; // Make the audio 2D for global chat
        }

        AudioClip clip = AudioClip.Create("ReceivedVoice", audioData.Length, 1, sampleRate, false);
        clip.SetData(audioData, 0);

        source.clip = clip;
        source.Play();
    }

    private Transform GetPlayerTransform(int clientId)
    {
        foreach (var obj in FindObjectsOfType<NetworkObject>())
        {
            if (obj.Owner.ClientId == clientId)
            {
                return obj.transform;
            }
        }
        return null;
    }

    private float GetMicInputVolume()
    {
        if (microphoneClip == null || string.IsNullOrEmpty(deviceName))
            return 0f;

        int micPosition = Microphone.GetPosition(deviceName);

        int sampleStartPosition = micPosition - bufferSize;
        if (sampleStartPosition < 0)
        {
            // Not enough data yet
            return 0f;
        }

        microphoneClip.GetData(micDataBuffer, sampleStartPosition);

        float sum = 0;
        for (int i = 0; i < micDataBuffer.Length; i++)
        {
            sum += micDataBuffer[i] * micDataBuffer[i]; // Squared values for RMS
        }

        float rmsValue = Mathf.Sqrt(sum / micDataBuffer.Length);

        float amplifiedVolume = Mathf.Clamp(rmsValue * 50f, 0f, 1f);
        return amplifiedVolume;
    }
}
