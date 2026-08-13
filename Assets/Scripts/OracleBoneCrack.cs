using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    [Header("掉落重置")]
    [Tooltip("只有已烧裂、但还没放到盘子上的甲骨掉到这个 Tag 的物体上时才会返回初始位置")]
    [SerializeField] private string groundTag = "Ground";

    [Header("放到盘子后的设置")]
    [SerializeField] private bool disableGrabAfterPlaced = true;

    // =========================
    // 事件
    // =========================

    // 开始接触火焰
    public event Action HeatingStarted;

    // 真正烧裂
    public event Action Cracked;

    // 成功放到盘子
    public event Action PlacedOnPlate;

    // =========================
    // 对外状态
    // =========================

    public bool IsCracked => isCracked;
    public bool IsPlacedOnPlate => isPlacedOnPlate;
    public bool IsHeating => isInsideFire;

    // =========================
    // 内部引用
    // =========================

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    // 抖动
    private Vector3 originalLocalPosition;

    // 初始位置
    private Vector3 startWorldPosition;
    private Quaternion startWorldRotation;
    private Transform startParent;

    // 加热
    private float heatingTime;
    private float nextHapticTime;

    // 状态
    private bool isInsideFire;
    private bool isCracked;
    private bool isPlacedOnPlate;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // 记录甲骨最开始的位置
        startWorldPosition = transform.position;
        startWorldRotation = transform.rotation;
        startParent = transform.parent;

        // 如果没有指定 Shake Target，就默认抖完整模型
        if (shakeTarget == null && completeModel != null)
        {
            shakeTarget = completeModel.transform;
        }

        if (shakeTarget != null)
        {
            originalLocalPosition = shakeTarget.localPosition;
        }

        // 初始模型状态
        if (completeModel != null)
        {
            completeModel.SetActive(true);
        }

        if (crackedModel != null)
        {
            crackedModel.SetActive(false);
        }
    }

    private void Update()
    {
        // 已经裂开，或者没有在火焰里，就不用继续加热
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

    // =========================
    // 火焰检测
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Fire"))
        {
            return;
        }

        if (isCracked)
        {
            return;
        }

        if (isInsideFire)
        {
            return;
        }

        isInsideFire = true;
        heatingTime = 0f;
        nextHapticTime = 0f;

        HeatingStarted?.Invoke();

        Debug.Log("[OracleBoneCrack] 甲骨开始加热。", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Fire"))
        {
            return;
        }

        // 已经裂开就不用再处理加热退出
        if (isCracked)
        {
            return;
        }

        StopHeating();
    }

    // =========================
    // 地面检测
    // =========================

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(groundTag))
        {
            return;
        }

        // 只有：
        // 1. 已经烧裂
        // 2. 还没有放到盘子
        // 才会返回原位
        if (isCracked && !isPlacedOnPlate)
        {
            ReturnToStartPosition();
        }
    }

    // =========================
    // 加热表现
    // =========================

    private void ShakeBone()
    {
        if (shakeTarget == null)
        {
            return;
        }

        float x =
            Mathf.Sin(Time.time * shakeSpeed)
            * shakeStrength;

        float y =
            Mathf.Cos(Time.time * shakeSpeed * 1.17f)
            * shakeStrength;

        float z =
            Mathf.Sin(Time.time * shakeSpeed * 0.83f)
            * shakeStrength;

        shakeTarget.localPosition =
            originalLocalPosition + new Vector3(x, y, z);
    }

    private void SendHapticPulse()
    {
        if (Time.time < nextHapticTime)
        {
            return;
        }

        nextHapticTime =
            Time.time + hapticInterval;

        // 没被抓住时不震手柄
        if (grabInteractable == null ||
            !grabInteractable.isSelected)
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

        ResetShakePosition();

        Debug.Log(
            "[OracleBoneCrack] 甲骨离开火焰。",
            this
        );
    }

    private void ResetShakePosition()
    {
        if (shakeTarget != null)
        {
            shakeTarget.localPosition =
                originalLocalPosition;
        }
    }

    // =========================
    // 裂开
    // =========================

    public void Crack()
    {
        if (isCracked)
        {
            return;
        }

        isCracked = true;
        isInsideFire = false;
        heatingTime = 0f;

        ResetShakePosition();

        if (completeModel != null)
        {
            completeModel.SetActive(false);
        }

        if (crackedModel != null)
        {
            crackedModel.SetActive(true);
        }

        Debug.Log(
            "[OracleBoneCrack] 甲骨裂开。",
            this
        );

        // 通知流程控制器
        Cracked?.Invoke();
    }

    // =========================
    // 放到盘子
    // =========================
    public void PlaceOnPlate(Transform plate)
    {
        if (!isCracked || isPlacedOnPlate)
        {
            return;
        }

        if (plate == null)
        {
            return;
        }

        isPlacedOnPlate = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // true = 保持当前世界坐标和旋转
        transform.SetParent(plate, true);

        if (disableGrabAfterPlaced &&
            grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        PlacedOnPlate?.Invoke();

        Debug.Log(
            "[OracleBoneCrack] 甲骨已固定到盘子，并保持松手位置。",
            this
        );
    }
    // =========================
    // 掉地后返回
    // =========================

    public void ReturnToStartPosition()
    {
        if (isPlacedOnPlate)
        {
            return;
        }

        // 停止物理运动
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 防止之前被某些系统改父级
        transform.SetParent(startParent);

        transform.position = startWorldPosition;
        transform.rotation = startWorldRotation;

        Debug.Log(
            "[OracleBoneCrack] 已烧裂甲骨掉地，返回初始位置。",
            this
        );

        // 注意：
        // 这里没有修改 isCracked
        // 所以回来以后仍然保持裂开状态
    }
}