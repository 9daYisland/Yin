using UnityEngine;

public class GazeAudioAction : GazeAction
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    [Header("Trigger")]
    [Tooltip("勾选：看满 Gaze Duration 后播放；取消：刚看到就播放")]
    [SerializeField] private bool playOnComplete = true;

    [Tooltip("移开视线时停止播放")]
    [SerializeField] private bool stopOnExit = false;

    [Tooltip("播放后不允许再次触发")]
    [SerializeField] private bool playOnlyOnce = false;

    [Tooltip("重新触发时，从头开始播放")]
    [SerializeField] private bool restartWhenTriggered = false;

    private bool hasPlayed;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;

            if (audioClip != null)
                audioSource.clip = audioClip;
        }
    }

    public override void OnGazeEnter()
    {
        if (!playOnComplete)
            PlayAudio();
    }

    public override void OnGazeComplete()
    {
        if (playOnComplete)
            PlayAudio();
    }

    public override void OnGazeExit()
    {
        if (stopOnExit && audioSource != null)
            audioSource.Stop();
    }

    public override void OnGazeReset()
    {
        
    }

    private void PlayAudio()
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 的 GazeAudioAction 没有 AudioSource。",
                this
            );
            return;
        }

        if (playOnlyOnce && hasPlayed)
            return;

        if (audioClip != null)
            audioSource.clip = audioClip;

        if (audioSource.clip == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 没有设置 AudioClip。",
                this
            );
            return;
        }

        if (audioSource.isPlaying)
        {
            if (!restartWhenTriggered)
                return;

            audioSource.Stop();
        }

        audioSource.Play();
        hasPlayed = true;
    }
}