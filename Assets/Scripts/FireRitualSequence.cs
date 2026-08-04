using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class FireRitualSequence : MonoBehaviour
{
    [Header("需要等待结束的 Timeline")]
    [SerializeField] private PlayableDirector targetTimeline;

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

    [Header("所有可以点燃的木头")]
    [SerializeField] private IgnitableObject[] ignitableObjects;

    [Header("甲骨")]
    [SerializeField] private OracleBoneCrack oracleBone;

    private bool woodHasBeenIgnited;
    private bool boneHasStartedHeating;
    private bool ignitionStageEnabled;

    private void Awake()
    {
        HideHint();

        foreach (IgnitableObject ignitable in ignitableObjects)
        {
            if (ignitable == null)
                continue;

            ignitable.SetIgnitionEnabled(false);
            ignitable.Ignited += OnWoodIgnited;
        }

        if (oracleBone != null)
        {
            oracleBone.HeatingStarted += OnBoneHeatingStarted;
        }

        if (targetTimeline != null)
        {
            targetTimeline.stopped += OnTimelineStopped;
        }
        else
        {
            Debug.LogWarning(
                "[FireRitualSequence] 没有设置 Target Timeline。",
                this
            );
        }
    }

    private void Start()
    {
        // Timeline 如果不是由其他脚本播放，可以在这里主动播放：
        // targetTimeline.Play();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != targetTimeline)
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

        Debug.Log(
            "[FireRitualSequence] Timeline 结束，允许点燃木头。",
            this
        );
    }

    private void OnWoodIgnited(IgnitableObject ignitedWood)
    {
        if (!ignitionStageEnabled || woodHasBeenIgnited)
            return;

        woodHasBeenIgnited = true;

        ShowHint(heatBoneHint);

        Debug.Log(
            $"[FireRitualSequence] {ignitedWood.name} 已点燃，切换 Hint。",
            ignitedWood
        );
    }

    private void OnBoneHeatingStarted()
    {
        if (!woodHasBeenIgnited || boneHasStartedHeating)
            return;

        boneHasStartedHeating = true;
        HideHint();

        Debug.Log(
            "[FireRitualSequence] 甲骨开始加热，隐藏 Hint。",
            this
        );
    }

    private void ShowHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }

        if (hintRoot != null)
        {
            hintRoot.SetActive(true);
        }
    }

    private void HideHint()
    {
        if (hintRoot != null)
        {
            hintRoot.SetActive(false);
        }
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
        }

        if (targetTimeline != null)
        {
            targetTimeline.stopped -= OnTimelineStopped;
        }
    }
}