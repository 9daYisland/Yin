using System.Collections;
using UnityEngine;

// 挂在场景里新建的一个空物体上,比如叫 "SequenceManager"
public class SequenceManager : MonoBehaviour
{
    [Header("主角组件（拖主角身上的组件进来）")]
    public Animator playerAnimator;
    public AudioSource playerAudioSource;

    [Header("音频片段（按顺序拖入 AudioClip）")]
    public AudioClip audio1; // 站立时播放
    public AudioClip audio2; // 走+转身 第一段配音
    public AudioClip audio3; // 走+转身 第二段配音
    public AudioClip audio4; // NPC下跪时播放
    public AudioClip audio5; // 最后额外播放的音频

    [Header("Animator 里的参数名")]
    public string walkTurnTrigger = "WalkTurn";

    [Header("场景里所有NPC（留空则自动查找）")]
    public NPCController[] npcList;

    [Header("时间设置")]
    public float openingDelay = 2f;

    public float kneelMaxRandomDelay = 0.4f;

    [Tooltip("audio4播放结束后，等待多少秒再播放audio5")]
    public float finalAudioDelay = 2f;


    void Start()
    {
        if (npcList == null || npcList.Length == 0)
        {
            npcList = FindObjectsOfType<NPCController>();
        }

        StartCoroutine(PlaySequence());
    }


    IEnumerator PlaySequence()
    {
        // 1. 开场停顿
        yield return new WaitForSeconds(openingDelay);


        // 2. 播放音频1
        playerAudioSource.clip = audio1;
        playerAudioSource.Play();

        yield return new WaitForSeconds(audio1.length);


        // 3. 触发走路+转身动画
        playerAnimator.SetTrigger(walkTurnTrigger);


        // 播放音频2
        playerAudioSource.clip = audio2;
        playerAudioSource.Play();

        yield return new WaitForSeconds(audio2.length);


        // 播放音频3
        playerAudioSource.clip = audio3;
        playerAudioSource.Play();

        yield return new WaitForSeconds(audio3.length);


        // 4. 播放音频4，同时NPC下跪
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

        // 等audio4播放结束
        yield return new WaitForSeconds(audio4.length);


        // 5. audio4结束后，自定义等待时间
        yield return new WaitForSeconds(finalAudioDelay);


        // 6. 播放最后一段audio5
        if (audio5 != null)
        {
            playerAudioSource.clip = audio5;
            playerAudioSource.Play();

            yield return new WaitForSeconds(audio5.length);
        }


        Debug.Log("整个开场流程播放完毕");
    }
}