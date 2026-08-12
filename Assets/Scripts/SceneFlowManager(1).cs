using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景管理器 —— 每个场景放一个，各管各的。
///
/// 转场判断方式（在 Inspector 里配置，直观可见）：
/// 把"这一幕结束时要播放的那段音频"对应的 AudioSource 和 AudioClip
/// 拖进 End Audio Source / End Audio Clip 两个字段。
/// 场景里其他音效再多都不影响 —— 只有这一个指定的 Clip 播完，才会触发转场。
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("这一幕的场景关系")]
    [Tooltip("下一幕场景名（需要和 Build Settings 里的名字一致），留空表示没有下一幕")]
    [SerializeField] private string nextScene;

    [Tooltip("上一幕场景名，留空表示没有上一幕")]
    [SerializeField] private string previousScene;

    [Header("默认切换设置")]
    [Tooltip("调用切换方法时如果没单独传延迟参数，使用这个默认值（秒）")]
    [SerializeField] private float defaultTransitionDelay = 1f;

    [Header("★ 用哪段音频判断这一幕结束（在这里拖，一目了然）")]
    [Tooltip("播放'结束音频'用的 AudioSource。场景里其他 AudioSource / 音效完全不影响判断，只看这一个。")]
    [SerializeField] private AudioSource endAudioSource;

    [Tooltip("具体是哪一段 Clip 代表这一幕结束。就算 End Audio Source 中途播过别的音效也不会误判，" +
             "只有当它播的正好是这个 Clip、并且播完了，才会触发转场。留空则表示：End Audio Source 只要\" +\n" +
             "\"不再播放（不管播的是什么）就触发，不推荐，容易误判。")]
    [SerializeField] private AudioClip endAudioClip;

    [Tooltip("是否启用上面这套'音频播完自动转场'的逻辑。不需要自动转场、只想手动调用代码控制，就关掉这个")]
    [SerializeField] private bool autoTransitionOnEndAudio = true;

    [Tooltip("End Audio Clip 播完之后，再额外等几秒才真正转场（0 = 不额外等，直接用默认延迟逻辑）")]
    [SerializeField] private float extraDelayAfterEndAudio = 0f;

    [Header("淡入淡出（可选，不需要可以不拖）")]
    [Tooltip("留空则不使用淡入淡出效果，直接按延迟时间切场景")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;
    private bool wasPlayingEndClip = false;

    private void Awake()
    {
        Instance = this;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!autoTransitionOnEndAudio) return;
        if (isTransitioning) return;
        if (endAudioSource == null) return;

        // 当前这一帧：End Audio Source 是不是正好在播放我们指定的那个 End Audio Clip
        bool isPlayingEndClip = endAudioSource.isPlaying &&
                                 (endAudioClip == null || endAudioSource.clip == endAudioClip);

        // 上一帧还在播 -> 这一帧不在播了 = 这段"结束音频"刚刚播完
        if (wasPlayingEndClip && !isPlayingEndClip)
        {
            Debug.Log("[SceneFlowManager] 结束音频播放完毕，准备转场。");
            GoToNextScene(extraDelayAfterEndAudio);
        }

        wasPlayingEndClip = isPlayingEndClip;
    }

    // ---------------- 手动切换场景（不想用自动检测也可以直接调这些方法）----------------

    public void GoToNextScene(float? delay = null)
    {
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogWarning($"[SceneFlowManager] 场景 '{GetCurrentSceneName()}' 没有配置下一幕（nextScene 为空）。");
            return;
        }
        GoToScene(nextScene, delay);
    }

    public void GoToPreviousScene(float? delay = null)
    {
        if (string.IsNullOrEmpty(previousScene))
        {
            Debug.LogWarning($"[SceneFlowManager] 场景 '{GetCurrentSceneName()}' 没有配置上一幕（previousScene 为空）。");
            return;
        }
        GoToScene(previousScene, delay);
    }

    public void GoToScene(string sceneName, float? delay = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneFlowManager] 正在切换场景中，忽略本次调用。");
            return;
        }
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneFlowManager] 目标场景名为空。");
            return;
        }

        float actualDelay = delay ?? defaultTransitionDelay;
        StartCoroutine(TransitionRoutine(sceneName, actualDelay));
    }

    public void ReloadCurrentScene(float? delay = null)
    {
        GoToScene(GetCurrentSceneName(), delay);
    }

    // ---------------- 内部实现 ----------------

    private IEnumerator TransitionRoutine(string sceneName, float delay)
    {
        isTransitioning = true;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
        {
            yield return null;
        }

        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = to;
    }

    private string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}
