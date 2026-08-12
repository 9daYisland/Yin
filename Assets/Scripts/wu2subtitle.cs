using UnityEngine;
using TMPro;

public class SubtitleManager : MonoBehaviour
{
    [System.Serializable]
    public class SubtitleLine
    {
        [TextArea(2, 5)]
        public string text;

        [Tooltip("这句字幕在当前音频播放到第几秒时出现")]
        public float startTime;

        [Tooltip("这句字幕显示多久")]
        public float duration = 2f;
    }

    [System.Serializable]
    public class AudioSubtitleGroup
    {
        [Header("对应的音频")]
        public AudioClip audioClip;

        [Header("这段音频的字幕")]
        public SubtitleLine[] subtitles;
    }

    [Header("和 SequenceManager 使用同一个 AudioSource")]
    public AudioSource playerAudioSource;

    [Header("字幕 TextMeshPro")]
    public TMP_Text subtitleText;

    [Header("每段音频对应的字幕")]
    public AudioSubtitleGroup[] audioGroups;

    private AudioClip lastClip;
    private int currentGroupIndex = -1;
    private int currentSubtitleIndex = -1;


    void Start()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }
    }


    void Update()
    {
        if (playerAudioSource == null || subtitleText == null)
            return;

        // 没有播放声音时，清空字幕
        if (!playerAudioSource.isPlaying)
        {
            ClearSubtitle();
            lastClip = null;
            currentGroupIndex = -1;
            return;
        }

        // 检测当前正在播放哪一个 AudioClip
        AudioClip currentClip = playerAudioSource.clip;

        if (currentClip == null)
        {
            ClearSubtitle();
            return;
        }

        // AudioClip 发生变化时，找到它对应的字幕组
        if (currentClip != lastClip)
        {
            lastClip = currentClip;
            currentGroupIndex = FindGroupForClip(currentClip);
            currentSubtitleIndex = -1;

            subtitleText.text = "";
        }

        // 没找到对应字幕组
        if (currentGroupIndex < 0)
        {
            ClearSubtitle();
            return;
        }

        float currentTime = playerAudioSource.time;

        SubtitleLine[] lines = audioGroups[currentGroupIndex].subtitles;

        int foundIndex = -1;

        // 找当前时间应该显示哪一句
        for (int i = 0; i < lines.Length; i++)
        {
            float start = lines[i].startTime;
            float end = start + lines[i].duration;

            if (currentTime >= start && currentTime < end)
            {
                foundIndex = i;
                break;
            }
        }

        // 只有字幕发生变化时才更新文字
        if (foundIndex != currentSubtitleIndex)
        {
            currentSubtitleIndex = foundIndex;

            if (foundIndex >= 0)
            {
                subtitleText.text = lines[foundIndex].text;
            }
            else
            {
                subtitleText.text = "";
            }
        }
    }


    int FindGroupForClip(AudioClip clip)
    {
        for (int i = 0; i < audioGroups.Length; i++)
        {
            if (audioGroups[i].audioClip == clip)
            {
                return i;
            }
        }

        return -1;
    }


    void ClearSubtitle()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        currentSubtitleIndex = -1;
    }
}