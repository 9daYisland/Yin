using System.Collections;
using UnityEngine;

// 挂在每一个 NPC 身上（做成 Prefab 后每个复制体都会自带这个脚本）
public class NPCController : MonoBehaviour
{
    public Animator animator;
    public string kneelTrigger = "Kneel";

    void Reset()
    {
        // 方便：复制粘贴出新NPC后不用每次手动拖 Animator
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Kneel()
    {
        if (animator != null)
            animator.SetTrigger(kneelTrigger);
    }

    // 延迟一小段时间后再跪下，用来错开多个NPC的动作
    public void KneelDelayed(float delay)
    {
        StartCoroutine(KneelAfterDelay(delay));
    }

    IEnumerator KneelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Kneel();
    }
}
