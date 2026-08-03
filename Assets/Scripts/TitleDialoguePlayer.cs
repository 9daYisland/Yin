using System.Collections;
using UnityEngine;
using TMPro; // 如果用普通 UI Text，把 TMP_Text 换成 UnityEngine.UI.Text，并去掉这行 using

/// <summary>
/// 独立播放器，跟场景切换完全无关，只负责这一套播放流程：
///
/// 1. 标题渐显
/// 2. 标题停留几秒后消失
/// 3. 同时播放音频1 + 音频2，同时显示字幕1 + 字幕2
/// 4. 音频1、2都播完后，等待几秒
/// 5. 播放字幕3 + 音频3
///
/// 全程不调用任何场景跳转逻辑，纯粹管播放。
/// </summary>
public class TitleDialoguePlayer : MonoBehaviour
{
    [Header("标题 UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup titleGroup;
    [TextArea] [SerializeField] private string titleContent;

    [Header("标题时间参数")]
    [SerializeField] private float titleFadeInDuration = 1f;   // 标题渐显用时
    [SerializeField] private float titleHoldDuration = 2f;     // 标题显示后停留几秒
    [SerializeField] private float titleFadeOutDuration = 1f;  // 标题消失用时

    [Header("字幕1 UI（配合音频1）")]
    [SerializeField] private TMP_Text subtitle1Text;
    [SerializeField] private CanvasGroup subtitle1Group; // 可留空，留空则不做淡入淡出，直接显示/隐藏
    [TextArea] [SerializeField] private string subtitle1;

    [Header("字幕2 UI（配合音频2）")]
    [SerializeField] private TMP_Text subtitle2Text;
    [SerializeField] private CanvasGroup subtitle2Group;
    [TextArea] [SerializeField] private string subtitle2;

    [Header("音频1 + 音频2（这两个会同时播放，所以要各用各的 AudioSource）")]
    [SerializeField] private AudioSource audioSource1;
    [SerializeField] private AudioClip audioClip1;
    [SerializeField] private AudioSource audioSource2;
    [SerializeField] private AudioClip audioClip2;

    [Header("音频1、2播完之后，等待几秒再播音频3")]
    [SerializeField] private float delayBeforeClip3 = 1f;

    [Header("字幕3 + 音频3")]
    [SerializeField] private TMP_Text subtitle3Text;
    [SerializeField] private CanvasGroup subtitle3Group;
    [TextArea] [SerializeField] private string subtitle3;
    [SerializeField] private AudioSource audioSource3; // 可以直接复用 audioSource1，因为这时候它已经播完空闲了
    [SerializeField] private AudioClip audioClip3;

    private void Start()
    {
        // 初始状态：标题、字幕全部隐藏
        if (titleGroup != null) titleGroup.alpha = 0f;
        SetGroupAlpha(subtitle1Group, 0f);
        SetGroupAlpha(subtitle2Group, 0f);
        SetGroupAlpha(subtitle3Group, 0f);

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // 1. 标题渐显
        if (titleText != null) titleText.text = titleContent;
        yield return StartCoroutine(Fade(titleGroup, 0f, 1f, titleFadeInDuration));

        // 2. 标题停留几秒
        yield return new WaitForSeconds(titleHoldDuration);

        // 3. 标题消失
        yield return StartCoroutine(Fade(titleGroup, 1f, 0f, titleFadeOutDuration));

        // 4. 同时播放音频1、音频2，同时显示字幕1、字幕2
        if (subtitle1Text != null) subtitle1Text.text = subtitle1;
        if (subtitle2Text != null) subtitle2Text.text = subtitle2;
        SetGroupAlpha(subtitle1Group, 1f);
        SetGroupAlpha(subtitle2Group, 1f);

        PlayClip(audioSource1, audioClip1);
        PlayClip(audioSource2, audioClip2);

        // 等两段音频都播完（谁播的时间长就等谁）
        yield return StartCoroutine(WaitUntilBothFinished(audioSource1, audioSource2));

        // 字幕1、2 隐藏
        SetGroupAlpha(subtitle1Group, 0f);
        SetGroupAlpha(subtitle2Group, 0f);

        // 5. 等待几秒
        if (delayBeforeClip3 > 0f)
        {
            yield return new WaitForSeconds(delayBeforeClip3);
        }

        // 6. 播放字幕3 + 音频3
        if (subtitle3Text != null) subtitle3Text.text = subtitle3;
        SetGroupAlpha(subtitle3Group, 1f);
        PlayClip(audioSource3, audioClip3);

        // 等音频3播完（如果你后面还有别的逻辑要接在这里，可以在这行之后继续写）
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

    private IEnumerator WaitUntilBothFinished(AudioSource a, AudioSource b)
    {
        yield return null; // 等一帧，确保两个 isPlaying 都已经生效
        while ((a != null && a.isPlaying) || (b != null && b.isPlaying))
        {
            yield return null;
        }
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
