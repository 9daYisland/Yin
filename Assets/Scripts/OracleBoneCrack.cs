using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System;
public class OracleBoneCrack : MonoBehaviour
{
    [Header("模型")]
    [SerializeField] private GameObject completeModel;
    [SerializeField] private GameObject crackedModel;

    [Header("裂开设置")]
    [SerializeField] private float crackDelay = 2f;

    [Header("甲骨抖动")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeStrength = 0.002f;
    [SerializeField] private float shakeSpeed = 35f;

    [Header("手柄震动")]
    [SerializeField] private HapticImpulsePlayer hapticImpulsePlayer;

    [Range(0f, 1f)]
    [SerializeField] private float hapticStrength = 0.2f;

    [SerializeField] private float hapticDuration = 0.06f;
    [SerializeField] private float hapticInterval = 0.15f;
    public event Action HeatingStarted;
    private XRGrabInteractable grabInteractable;

    private Vector3 originalLocalPosition;
    private float heatingTime;
    private float nextHapticTime;

    private bool isInsideFire;
    private bool isCracked;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (shakeTarget == null && completeModel != null)
        {
            shakeTarget = completeModel.transform;
        }

        if (shakeTarget != null)
        {
            originalLocalPosition = shakeTarget.localPosition;
        }

        completeModel.SetActive(true);
        crackedModel.SetActive(false);
    }

    private void Update()
    {
        if (isCracked || !isInsideFire)
        {
            return;
        }

        heatingTime += Time.deltaTime;

        ShakeBone();
        SendHapticPulse();

        if (heatingTime >= crackDelay)
        {
            Crack();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Fire") || isCracked)
        {
            return;
        }

        if (isInsideFire)
        {
            return;
        }

        isInsideFire = true;
        heatingTime = 0f;

        HeatingStarted?.Invoke();

        Debug.Log("[OracleBoneCrack] 甲骨开始加热。");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Fire"))
        {
            return;
        }

        StopHeating();
    }

    private void ShakeBone()
    {
        if (shakeTarget == null)
        {
            return;
        }

        float x = Mathf.Sin(Time.time * shakeSpeed) * shakeStrength;
        float y = Mathf.Cos(Time.time * shakeSpeed * 1.17f) * shakeStrength;
        float z = Mathf.Sin(Time.time * shakeSpeed * 0.83f) * shakeStrength;

        shakeTarget.localPosition =
            originalLocalPosition + new Vector3(x, y, z);
    }

    private void SendHapticPulse()
    {
        if (Time.time < nextHapticTime)
        {
            return;
        }

        nextHapticTime = Time.time + hapticInterval;

        // 只有甲骨当前被抓住时才震动手柄
        if (grabInteractable == null || !grabInteractable.isSelected)
        {
            return;
        }

        if (hapticImpulsePlayer != null)
        {
            hapticImpulsePlayer.SendHapticImpulse(
                hapticStrength,
                hapticDuration
            );
        }
    }

    private void StopHeating()
    {
        isInsideFire = false;
        heatingTime = 0f;

        if (shakeTarget != null)
        {
            shakeTarget.localPosition = originalLocalPosition;
        }

        Debug.Log("甲骨离开火焰");
    }

    public void Crack()
    {
        if (isCracked)
        {
            return;
        }

        isCracked = true;
        isInsideFire = false;

        if (shakeTarget != null)
        {
            shakeTarget.localPosition = originalLocalPosition;
        }

        completeModel.SetActive(false);
        crackedModel.SetActive(true);

        Debug.Log("甲骨裂开");
    }
}