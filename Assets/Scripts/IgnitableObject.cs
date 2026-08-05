using System;
using UnityEngine;

public class IgnitableObject : MonoBehaviour
{
    [Header("点燃后出现的对象")]
    [SerializeField] private GameObject[] fireObjects;

    [Header("点燃设置")]
    [SerializeField] private string torchTag = "Torch";
    [SerializeField] private bool canIgniteOnlyOnce = true;

    [Tooltip("游戏开始时是否允许点燃。当前流程建议关闭。")]
    [SerializeField] private bool ignitionEnabledAtStart = false;

    private bool isIgnited;
    private bool ignitionEnabled;

    public bool IsIgnited => isIgnited;
    public bool IgnitionEnabled => ignitionEnabled;

    public event Action<IgnitableObject> Ignited;

    private void Awake()
    {
        ignitionEnabled = ignitionEnabledAtStart;


        foreach (GameObject fireObject in fireObjects)
        {
            if (fireObject != null)
            {
                fireObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryIgnite(other);
    }

    private void TryIgnite(Collider other)
    {
        if (!ignitionEnabled)
        {
            return;
        }

        if (isIgnited && canIgniteOnlyOnce)
        {
            return;
        }

        if (!other.CompareTag(torchTag))
        {
            return;
        }

        Ignite();
    }

    public void SetIgnitionEnabled(bool enabled)
    {
        ignitionEnabled = enabled;

    }

    public void Ignite()
    {
        if (!ignitionEnabled)
        {
            return;
        }

        if (isIgnited && canIgniteOnlyOnce)
        {
            return;
        }

        isIgnited = true;

        foreach (GameObject fireObject in fireObjects)
        {
            if (fireObject != null)
            {
                fireObject.SetActive(true);
            }
        }


        Ignited?.Invoke(this);
    }
}