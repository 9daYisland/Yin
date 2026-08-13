using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneFlowController : MonoBehaviour
{
    [Header("Timelines")]
    [SerializeField] private PlayableDirector timeline1;
    [SerializeField] private PlayableDirector timeline2;
    [SerializeField] private PlayableDirector timeline3;

    [Header("Fire")]
    [SerializeField] private FireController fireController;

    [Header("Fire Interaction")]
    [SerializeField] private Outline fireOutline;

    [Tooltip("火堆描边显示时的宽度")]
    [SerializeField] private float fireOutlineWidth = 5f;

    [Header("Hint UI")]
    [Tooltip("拖 HintCanvas 根对象")]
    [SerializeField] private GameObject hintCanvas;

    [Tooltip("拖 HintCanvas/Panel/Text (TMP)")]
    [SerializeField] private TMP_Text hintText;

    [TextArea]
    [SerializeField]
    private string igniteHint =
        "Light the fire.";

    [Header("Scene Dissolve")]
    [SerializeField] private SceneDissolveController dissolveController;

    [Tooltip("等待溶解完成的时间")]
    [SerializeField] private float dissolveDuration = 5f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName;

    private bool waitingForFire = false;
    private bool fireTriggered = false;
    public bool CanIgniteFire
    {
        get
        {
            return waitingForFire && !fireTriggered;
        }
    }

    private void Start()
    {
        // =========================================
        // 开场火堆必须是熄灭状态
        // =========================================

        if (fireController != null)
        {
            fireController.SetExtinguishedInstantly();
        }

        // =========================================
        // 开场隐藏 Hint
        // =========================================

        HideHint();

        // =========================================
        // 开场隐藏 Outline
        // 不关闭组件，只把宽度设为0
        // =========================================

        if (fireOutline != null)
        {
            fireOutline.OutlineWidth = 0f;
        }

        // =========================================
        // 开始整个场景流程
        // =========================================

        StartCoroutine(SceneSequence());
    }

    // =========================================================
    // 整个 Scene 流程
    // =========================================================

    private IEnumerator SceneSequence()
    {
        // =====================================================
        // 1. Timeline1
        // =====================================================

        if (timeline1 != null)
        {
            timeline1.Play();

            yield return WaitForTimeline(timeline1);
        }

        // =====================================================
        // 2. Timeline1结束
        // 显示点火提示 + 描边
        // =====================================================

        ShowFireInteraction();

        // 等玩家点火
        while (!fireTriggered)
        {
            yield return null;
        }

        // =====================================================
        // 3. 玩家点火
        // =====================================================

        HideFireInteraction();

        // 火逐渐点亮
        if (fireController != null)
        {
            fireController.FadeIn();
        }

        // =====================================================
        // Timeline2
        // =====================================================

        if (timeline2 != null)
        {
            timeline2.Play();

            yield return WaitForTimeline(timeline2);
        }

        // =====================================================
        // 4. Timeline2结束
        // 火逐渐熄灭，同时播放 Timeline3
        // =====================================================

        if (fireController != null)
        {
            fireController.FadeOut();
        }

        if (timeline3 != null)
        {
            timeline3.Play();

            yield return WaitForTimeline(timeline3);
        }

        // =====================================================
        // 5. Timeline3结束
        // 整个场景 Dissolve
        // =====================================================

        if (dissolveController != null)
        {
            dissolveController.PlayDissolve();
        }

        yield return new WaitForSeconds(dissolveDuration);

        // =====================================================
        // 6. 切换下一个 Scene
        // =====================================================

        LoadNextScene();
    }

    // =========================================================
    // 等待 Timeline 播放结束
    // =========================================================

    private IEnumerator WaitForTimeline(PlayableDirector director)
    {
        if (director == null)
            yield break;

        // 先等一帧，确保 PlayableDirector state 更新
        yield return null;

        while (
            director != null &&
            director.state == PlayState.Playing
        )
        {
            yield return null;
        }
    }

    // =========================================================
    // 显示点火交互
    // =========================================================

    private void ShowFireInteraction()
    {
        waitingForFire = true;

        // 显示 Hint
        ShowHint(igniteHint);

        // 显示描边
        if (fireOutline != null)
        {
            fireOutline.OutlineWidth =
                fireOutlineWidth;
        }
    }

    // =========================================================
    // 隐藏点火交互
    // =========================================================

    private void HideFireInteraction()
    {
        waitingForFire = false;

        // 隐藏 Hint
        HideHint();

        // 隐藏描边
        if (fireOutline != null)
        {
            fireOutline.OutlineWidth = 0f;
        }
    }

    // =========================================================
    // Hint UI
    // =========================================================

    private void ShowHint(string message)
    {
        // 先修改 TMP 内容
        if (hintText != null)
        {
            hintText.text = message;
        }
        else
        {
            Debug.LogWarning(
                "[SceneFlow] Hint Text 没有绑定"
            );
        }

        // 再打开整个 HintCanvas
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[SceneFlow] Hint Canvas 没有绑定"
            );
        }
    }

    private void HideHint()
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(false);
        }
    }

    // =========================================================
    // FireIgnitionTrigger 调用这个
    // =========================================================

    public bool TryIgniteFire()
    {
        // Timeline1 还没结束
        if (!waitingForFire)
            return false;

        // 已经成功点过了
        if (fireTriggered)
            return false;

        fireTriggered = true;

        Debug.Log("[SceneFlow] Fire Ignited");

        return true;
    }

    // =========================================================
    // 切换 Scene
    // =========================================================

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning(
                "[SceneFlow] Next Scene Name 没有填写"
            );

            return;
        }

        SceneManager.LoadScene(
            nextSceneName
        );
    }
}