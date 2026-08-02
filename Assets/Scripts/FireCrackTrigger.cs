using System.Collections.Generic;
using UnityEngine;

public class FireCrackTrigger : MonoBehaviour
{
    [Header("甲骨在火焰中停留多久后裂开")]
    [SerializeField] private float crackDelay = 1.5f;

    [Header("这一整组木头的根物体")]
    [SerializeField] private Transform woodRoot;

    [Header("是否输出调试信息")]
    [SerializeField] private bool showDebugLogs = true;

    private IgnitableObject[] ignitableObjects;

    // 支持甲骨有多个 Collider，避免 currentBone 被覆盖或提前清空
    private readonly Dictionary<OracleBoneCrack, float> boneTimers =
        new Dictionary<OracleBoneCrack, float>();

    private void Awake()
    {
        if (woodRoot == null)
        {
            woodRoot = transform.parent;
        }

        if (woodRoot == null)
        {
            Debug.LogError(
                $"[FireCrackTrigger] {name} 找不到 Wood Root。",
                this
            );
            return;
        }

        ignitableObjects =
            woodRoot.GetComponentsInChildren<IgnitableObject>(true);

        Debug.Log(
            $"[FireCrackTrigger] 在 {woodRoot.name} 下找到 " +
            $"{ignitableObjects.Length} 个 IgnitableObject。",
            this
        );

        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            Debug.Log(
                $"[FireCrackTrigger] 找到木头：{ignitable.name}",
                ignitable
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                $"[FireCrackTrigger] 有物体进入：{other.name}，" +
                $"Root：{other.transform.root.name}",
                other
            );
        }

        OracleBoneCrack bone =
            other.GetComponentInParent<OracleBoneCrack>();

        if (bone == null)
        {
            Debug.LogWarning(
                $"[FireCrackTrigger] {other.name} 进入了火焰，" +
                $"但其父级中找不到 OracleBoneCrack。",
                other
            );

            return;
        }

        if (!boneTimers.ContainsKey(bone))
        {
            boneTimers.Add(bone, 0f);

            Debug.Log(
                $"[FireCrackTrigger] 已识别甲骨：{bone.name}",
                bone
            );
        }
    }

    private void OnTriggerStay(Collider other)
    {
        OracleBoneCrack bone =
            other.GetComponentInParent<OracleBoneCrack>();

        if (bone == null)
        {
            return;
        }

        if (!boneTimers.ContainsKey(bone))
        {
            boneTimers.Add(bone, 0f);
        }

        bool fireIsLit = IsAnyWoodIgnited();

        if (!fireIsLit)
        {
            boneTimers[bone] = 0f;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[FireCrackTrigger] 甲骨 {bone.name} 在火焰区，" +
                    $"但没有检测到已点燃的木头。",
                    this
                );
            }

            return;
        }

        boneTimers[bone] += Time.deltaTime;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[FireCrackTrigger] 正在加热 {bone.name}：" +
                $"{boneTimers[bone]:F2} / {crackDelay:F2}",
                this
            );
        }

        if (boneTimers[bone] >= crackDelay)
        {
            Debug.Log(
                $"[FireCrackTrigger] 调用 {bone.name}.Crack()",
                bone
            );

            bone.Crack();
            boneTimers.Remove(bone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                $"[FireCrackTrigger] 有物体离开：{other.name}",
                other
            );
        }

        OracleBoneCrack bone =
            other.GetComponentInParent<OracleBoneCrack>();

        if (bone != null && boneTimers.ContainsKey(bone))
        {
            boneTimers.Remove(bone);

            Debug.Log(
                $"[FireCrackTrigger] 甲骨 {bone.name} 离开，计时清零。",
                bone
            );
        }
    }

    private bool IsAnyWoodIgnited()
    {
        if (ignitableObjects == null || ignitableObjects.Length == 0)
        {
            return false;
        }

        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            if (ignitable == null)
            {
                continue;
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[FireCrackTrigger] 检查 {ignitable.name}：" +
                    $"IsIgnited = {ignitable.IsIgnited}",
                    ignitable
                );
            }

            if (ignitable.IsIgnited)
            {
                return true;
            }
        }

        return false;
    }
}