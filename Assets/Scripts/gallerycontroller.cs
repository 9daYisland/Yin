using System.Collections;
using UnityEngine;

public class VRGalleryController : MonoBehaviour
{
    [Header("按出现顺序拖入所有展板")]
    [SerializeField]
    private CanvasGroup[] panels;

    [Header("开始展示前的等待时间")]
    [SerializeField]
    private float startDelay = 0f;

    [Header("每块展板渐显需要多少秒")]
    [SerializeField]
    private float revealDuration = 1f;

    [Header("每块展板完全出现后的间隔")]
    [SerializeField]
    private float interval = 2f;


    private bool hasStarted = false;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // 场景开始时所有展板隐藏
        HideAllPanels();
    }


    // =========================================================
    // 隐藏全部展板
    // =========================================================

    private void HideAllPanels()
    {
        if (panels == null)
        {
            return;
        }


        foreach (CanvasGroup panel in panels)
        {
            if (panel == null)
            {
                continue;
            }


            panel.alpha = 0f;

            panel.interactable = false;

            panel.blocksRaycasts = false;
        }
    }


    // =========================================================
    // 外部调用这个函数
    // 开始依次显示展板
    // =========================================================

    public void StartGallery()
    {
        if (hasStarted)
        {
            return;
        }


        hasStarted = true;


        StartCoroutine(
            GallerySequence()
        );


        Debug.Log(
            "[Gallery] 开始显示展板。"
        );
    }


    // =========================================================
    // 展板完整流程
    // =========================================================

    private IEnumerator GallerySequence()
    {
        // 如果需要，可以先等一下
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(
                startDelay
            );
        }


        if (panels == null)
        {
            yield break;
        }


        // 按 Inspector 数组顺序出现
        foreach (CanvasGroup panel in panels)
        {
            if (panel == null)
            {
                continue;
            }


            // 渐显当前展板
            yield return RevealPanel(
                panel
            );


            // 当前展板出现后
            // 等一段时间再显示下一个
            if (interval > 0f)
            {
                yield return new WaitForSeconds(
                    interval
                );
            }
        }


        Debug.Log(
            "[Gallery] 所有展板显示完成。"
        );
    }


    // =========================================================
    // 单个展板渐显
    // =========================================================

    private IEnumerator RevealPanel(
        CanvasGroup panel)
    {
        float timer = 0f;


        // 防止 Reveal Duration = 0
        if (revealDuration <= 0f)
        {
            panel.alpha = 1f;

            yield break;
        }


        while (timer < revealDuration)
        {
            timer += Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    timer /
                    revealDuration
                );


            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );


            panel.alpha =
                progress;


            yield return null;
        }


        panel.alpha = 1f;
    }
}