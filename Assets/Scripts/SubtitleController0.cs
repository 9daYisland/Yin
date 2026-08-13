using System.Collections;
using UnityEngine;
using TMPro;

public class WordByWordSubtitle : MonoBehaviour
{
    [Header("Subtitle")]
    public TMP_Text subtitleText;

    [TextArea(3, 10)]
    public string fullSentence;

    [Header("Timing")]
    public float wordInterval = 0.3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip audioClip;

    private Coroutine subtitleCoroutine;

    void Start()
    {
        subtitleText.text = "";

        PlaySubtitle();
    }

    public void PlaySubtitle()
    {
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
        }

        subtitleCoroutine = StartCoroutine(PlaySubtitleCoroutine());
    }

    IEnumerator PlaySubtitleCoroutine()
    {
        subtitleText.text = "";

        // 播放音乐 / 音频
        if (audioSource != null && audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }

        // 按空格拆成一个个单词
        string[] words = fullSentence.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            if (i == 0)
            {
                subtitleText.text = words[i];
            }
            else
            {
                subtitleText.text += " " + words[i];
            }

            yield return new WaitForSeconds(wordInterval);
        }
    }
}