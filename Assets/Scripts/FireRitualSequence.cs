using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class FireRitualSequence : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector timeline1;
    [SerializeField] private PlayableDirector timeline2;
    [SerializeField] private PlayableDirector timeline4;

    [Header("是否由这个脚本启动 Timeline1")]
    [SerializeField] private bool playTimeline1OnStart = true;

    [Header("Hint UI")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private TMP_Text hintText;

    [Header("Hint 内容")]
    [TextArea]
    [SerializeField]
    private string lightWoodHint =
        "Use the torch to light the firewood.";

    [TextArea]
    [SerializeField]
    private string heatBoneHint =
        "Place the oracle bone over the fire.";

    [TextArea]
    [SerializeField]
    private string plateHint =
        "Place the oracle bone on the plate held by the diviner on your left.";

    [Header("所有可以点燃的木头")]
    [SerializeField] private IgnitableObject[] ignitableObjects;

    [Header("甲骨")]
    [SerializeField] private OracleBoneCrack oracleBone;

    private bool woodHasBeenIgnited;
    private bool boneHasStartedHeating;
    private bool boneHasCracked;
    private bool ignitionStageEnabled;

    [Header("Timeline4 结束后切换场景")]
    [SerializeField] private string nextSceneName;

    private void Awake()
    {
        HideHint();

        // 游戏开始时禁止点火
        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            if (ignitable == null)
                continue;

            ignitable.SetIgnitionEnabled(false);
            ignitable.Ignited += OnWoodIgnited;
        }

        // 监听甲骨事件
        if (oracleBone != null)
        {
            oracleBone.HeatingStarted += OnBoneHeatingStarted;
            oracleBone.Cracked += OnBoneCracked;
            oracleBone.PlacedOnPlate += OnBonePlacedOnPlate;
        }

        // Timeline 事件
        if (timeline1 != null)
            timeline1.stopped += OnTimeline1Stopped;

        if (timeline2 != null)
            timeline2.stopped += OnTimeline2Stopped;
        if (timeline4 != null)
            timeline4.stopped += OnTimeline4Stopped;
    }

    private void Start()
    {
        if (playTimeline1OnStart && timeline1 != null)
        {
            timeline1.Play();
        }
    }

    // Timeline1结束 → Timeline2开始
    private void OnTimeline1Stopped(PlayableDirector director)
    {
        if (director != timeline1)
            return;

        if (timeline2 != null)
        {
            timeline2.Play();
        }
        else
        {
            EnableWoodIgnition();
        }
    }

    // Timeline2结束 → 正式开始交互
    private void OnTimeline2Stopped(PlayableDirector director)
    {
        if (director != timeline2)
            return;

        EnableWoodIgnition();
    }

    private void EnableWoodIgnition()
    {
        if (ignitionStageEnabled)
            return;

        ignitionStageEnabled = true;

        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            if (ignitable != null)
            {
                ignitable.SetIgnitionEnabled(true);
            }
        }

        ShowHint(lightWoodHint);

        Debug.Log("[Sequence] Timeline2结束，允许点燃木头。");
    }

    // 木头点燃
    private void OnWoodIgnited(IgnitableObject ignitedWood)
    {
        if (!ignitionStageEnabled || woodHasBeenIgnited)
            return;

        woodHasBeenIgnited = true;

        ShowHint(heatBoneHint);

        Debug.Log("[Sequence] 木头点燃，提示灼烧甲骨。");
    }

    // 甲骨刚进入火焰
    private void OnBoneHeatingStarted()
    {
        if (!woodHasBeenIgnited || boneHasStartedHeating)
            return;

        boneHasStartedHeating = true;

        HideHint();

        Debug.Log("[Sequence] 甲骨开始灼烧，隐藏提示。");
    }

    // 甲骨真正裂开
    private void OnBoneCracked()
    {
        if (boneHasCracked)
            return;

        boneHasCracked = true;

        ShowHint(plateHint);

        Debug.Log("[Sequence] 甲骨烧裂，提示放到盘子上。");
    }

    // 成功放进盘子
    private void OnBonePlacedOnPlate()
    {
        HideHint();

        Debug.Log("[Sequence] 甲骨已经放到盘子上，播放 Timeline4。");

        if (timeline4 != null)
        {
            timeline4.time = 0;
            timeline4.Play();
        }
        else
        {
            Debug.LogWarning(
                "[Sequence] 没有设置 Timeline4。",
                this
            );
        }
    }

    private void ShowHint(string message)
    {
        if (hintText != null)
            hintText.text = message;

        if (hintRoot != null)
            hintRoot.SetActive(true);
    }

    private void HideHint()
    {
        if (hintRoot != null)
            hintRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            if (ignitable != null)
            {
                ignitable.Ignited -= OnWoodIgnited;
            }
        }

        if (oracleBone != null)
        {
            oracleBone.HeatingStarted -= OnBoneHeatingStarted;
            oracleBone.Cracked -= OnBoneCracked;
            oracleBone.PlacedOnPlate -= OnBonePlacedOnPlate;
        }

        if (timeline1 != null)
            timeline1.stopped -= OnTimeline1Stopped;

        if (timeline2 != null)
            timeline2.stopped -= OnTimeline2Stopped;
        if (timeline4 != null)
            timeline4.stopped -= OnTimeline4Stopped;
    }
    private void OnTimeline4Stopped(PlayableDirector director)
    {
        if (director != timeline4)
            return;

        Debug.Log("[Sequence] Timeline4 播放结束，切换场景。");

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning(
                "[Sequence] Next Scene Name 没有填写。",
                this
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}