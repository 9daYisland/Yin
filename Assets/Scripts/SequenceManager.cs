using System.Collections;
using UnityEngine;

// 挂在场景里新建的一个空物体上,比如叫 "SequenceManager"
public class SequenceManager : MonoBehaviour
{
    [Header("主角组件（拖主角身上的组件进来）")]
    public Animator playerAnimator;       // 主角身上的 Animator
    public AudioSource playerAudioSource; // 主角身上的 AudioSource

    [Header("音频片段（按顺序拖入 AudioClip）")]
    public AudioClip audio1; // 站立时播放
    public AudioClip audio2; // 走+转身 第一段配音
    public AudioClip audio3; // 走+转身 第二段配音
    public AudioClip audio4; // NPC下跪时播放

    [Header("Animator 里的参数名，要和 Animator Controller 里建的一致")]
    // 主角只需要1个触发器：从"呼吸待机"切到"走路+转身"
    // （呼吸待机是 Animator 的默认状态，一进场景就会自动播放，不用触发）
    public string walkTurnTrigger = "WalkTurn";

    [Header("场景里所有NPC（留空则自动查找）")]
    public NPCController[] npcList;

    [Header("时间设置")]
    public float openingDelay = 2f; // 开场停顿时长（秒）
    public float kneelMaxRandomDelay = 0.4f; // NPC下跪的最大随机延迟范围（秒），让每个NPC稍微错开

    void Start()
    {
        if (npcList == null || npcList.Length == 0)
        {
            // 自动找场景里所有挂了 NPCController 的物体
            npcList = FindObjectsOfType<NPCController>();
        }

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // 1. 开场停顿（这期间主角默认就在播放"呼吸待机"动画，不用触发）
        yield return new WaitForSeconds(openingDelay);

        // 2. 播放音频1（呼吸待机动画持续播放中）
        playerAudioSource.clip = audio1;
        playerAudioSource.Play();
        yield return new WaitForSeconds(audio1.length);

        // 3. 音频1结束 -> 触发"走路+转身"动画，同时依次播放音频2、音频3
        playerAnimator.SetTrigger(walkTurnTrigger);

        playerAudioSource.clip = audio2;
        playerAudioSource.Play();
        yield return new WaitForSeconds(audio2.length);

        playerAudioSource.clip = audio3;
        playerAudioSource.Play();
        yield return new WaitForSeconds(audio3.length);

        // 转身动画本身不循环，播完会自动停在最后一帧定格，不用再手动切换状态

        // 4. 播放音频4，同时所有NPC做 站立->跪下
        playerAudioSource.clip = audio4;
        playerAudioSource.Play();

        foreach (var npc in npcList)
        {
            if (npc != null)
            {
                float delay = Random.Range(0f, kneelMaxRandomDelay);
                npc.KneelDelayed(delay);
            }
        }

        yield return new WaitForSeconds(audio4.length);

        Debug.Log("整个开场流程播放完毕");
    }
}
