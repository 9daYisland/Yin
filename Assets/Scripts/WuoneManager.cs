using System.Collections;
using UnityEngine;
using TMPro; // 如果用普通 UI Text，把 TMP_Text 换成 UnityEngine.UI.Text，并去掉这行 using

/// <summary>
/// 独立播放器，跟场景切换无关，只负责这一套流程：
///
/// 1. 播放音频1，同时显示字幕1
/// 2. 音频1播放几秒后，单独出现"提示字幕"
/// 3. 提示字幕停留几秒后消失
/// 4. 等音频1播完 -> 播放音频2，同时显示字幕2
/// 5. 音频2播完后，停顿几秒
/// 6. 播放音频3，同时显示字幕3
/// </summary>
public class WuoneManager : MonoBehaviour
{
    [Header("音频1 + 字幕1")]
    [SerializeField] private AudioSource audioSource1;
    [SerializeField] private AudioClip audioClip1;
    [SerializeField] private TMP_Text subtitle1Text;
    [SerializeField] private CanvasGroup subtitle1Group; // 可留空，留空则直接显隐不做淡入淡出
    [TextArea][SerializeField] private string subtitle1;

    [Header("提示字幕（音频1播放过程中单独出现一次）")]
    [Tooltip("音频1开始播放后，等几秒再出现提示字幕")]
    [SerializeField] private float delayBeforeHint = 2f;
    [Tooltip("提示字幕出现后，停留几秒自动消失")]
    [SerializeField] private float hintDisplayDuration = 2f;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private CanvasGroup hintGroup;
    [TextArea][SerializeField] private string hintSubtitle;

    [Header("音频2 + 字幕2")]
    [Tooltip("音频1完全播完（含提示字幕流程）后，再等几秒才开始播音频2")]
    [SerializeField] private float delayBeforeClip2 = 1f;
    [SerializeField] private AudioSource audioSource2;
    [SerializeField] private AudioClip audioClip2;
    [SerializeField] private TMP_Text subtitle2Text;
    [SerializeField] private CanvasGroup subtitle2Group;
    [TextArea][SerializeField] private string subtitle2;

    [Header("音频2播完后，停顿几秒再播音频3")]
    [SerializeField] private float delayBeforeClip3 = 1f;

    [Header("音频3 + 字幕3")]
    [SerializeField] private AudioSource audioSource3;
    [SerializeField] private AudioClip audioClip3;
    [SerializeField] private TMP_Text subtitle3Text;
    [SerializeField] private CanvasGroup subtitle3Group;
    [TextArea][SerializeField] private string subtitle3;

    private void Start()
    {
        // 初始状态：所有字幕都隐藏
        SetGroupAlpha(subtitle1Group, 0f);
        SetGroupAlpha(hintGroup, 0f);
        SetGroupAlpha(subtitle2Group, 0f);
        SetGroupAlpha(subtitle3Group, 0f);

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // 1. 播放音频1，同时显示字幕1
        if (subtitle1Text != null) subtitle1Text.text = subtitle1;
        SetGroupAlpha(subtitle1Group, 1f);
        PlayClip(audioSource1, audioClip1);

        // 2. 音频1播放几秒后，出现提示字幕
        if (delayBeforeHint > 0f)
        {
            yield return new WaitForSeconds(delayBeforeHint);
        }
        if (hintText != null) hintText.text = hintSubtitle;
        SetGroupAlpha(hintGroup, 1f);

        // 3. 提示字幕停留几秒后消失
        yield return new WaitForSeconds(hintDisplayDuration);
        SetGroupAlpha(hintGroup, 0f);

        // 保险起见：如果提示字幕的时间比音频1还短，这里补等音频1播完，
        // 避免音频1还没放完就提前切到音频2
        yield return StartCoroutine(WaitUntilFinished(audioSource1));

        // 音频1对应的字幕1也隐藏掉
        SetGroupAlpha(subtitle1Group, 0f);

        // 音频1完全结束后，再等几秒才开始播音频2
        if (delayBeforeClip2 > 0f)
        {
            yield return new WaitForSeconds(delayBeforeClip2);
        }

        // 4. 播放音频2，同时显示字幕2
        if (subtitle2Text != null) subtitle2Text.text = subtitle2;
        SetGroupAlpha(subtitle2Group, 1f);
        PlayClip(audioSource2, audioClip2);

        yield return StartCoroutine(WaitUntilFinished(audioSource2));
        SetGroupAlpha(subtitle2Group, 0f);

        // 5. 音频2播完后，停顿几秒
        if (delayBeforeClip3 > 0f)
        {
            yield return new WaitForSeconds(delayBeforeClip3);
        }

        // 6. 播放音频3，同时显示字幕3
        if (subtitle3Text != null) subtitle3Text.text = subtitle3;
        SetGroupAlpha(subtitle3Group, 1f);
        PlayClip(audioSource3, audioClip3);

        yield return StartCoroutine(WaitUntilFinished(audioSource3));
    }

    // ---------------- 工具方法 ----------------

    private void PlayClip(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.Play();
    }

    private IEnumerator WaitUntilFinished(AudioSource source)
    {
        if (source == null) yield break;
        yield return null; // 等一帧，确保 isPlaying 已经生效
        while (source.isPlaying)
        {
            yield return null;
        }
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
    }
}
