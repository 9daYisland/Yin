using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OracleBonePlate : MonoBehaviour
{
    [Header("甲骨最终跟随的盘子")]
    [SerializeField] private Transform plate;

    private OracleBoneCrack boneInside;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        if (plate == null)
        {
            plate = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OracleBoneCrack bone =
            other.GetComponentInParent<OracleBoneCrack>();

        if (bone == null)
        {
            return;
        }

        // 必须已经烧裂
        if (!bone.IsCracked)
        {
            return;
        }

        // 已经放到盘子上了
        if (bone.IsPlacedOnPlate)
        {
            return;
        }

        // 防止同一块甲骨多个 Collider 重复注册
        if (boneInside == bone)
        {
            return;
        }

        // 如果之前已经监听了别的甲骨，先清掉
        RemoveReleaseListener();

        boneInside = bone;

        grabInteractable =
            bone.GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogWarning(
                "[OracleBonePlate] 找到了甲骨，但甲骨根物体上没有 XRGrabInteractable。",
                bone
            );

            return;
        }

        // XRI 3.x 用 AddListener
        grabInteractable.selectExited.AddListener(OnBoneReleased);

        Debug.Log(
            "[OracleBonePlate] 裂开的甲骨进入盘子范围，等待玩家松手。",
            this
        );

        // 如果甲骨进入盘子时本来就已经没有被抓住
        // 直接认为已经放下
        if (!grabInteractable.isSelected)
        {
            PlaceBone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OracleBoneCrack bone =
            other.GetComponentInParent<OracleBoneCrack>();

        if (bone == null)
        {
            return;
        }

        if (bone != boneInside)
        {
            return;
        }

        // 已经正式放到盘子上的甲骨，不再处理退出
        if (bone.IsPlacedOnPlate)
        {
            return;
        }

        RemoveReleaseListener();

        boneInside = null;
        grabInteractable = null;

        Debug.Log(
            "[OracleBonePlate] 甲骨离开盘子范围。",
            this
        );
    }

    private void OnBoneReleased(SelectExitEventArgs args)
    {
        if (boneInside == null)
        {
            return;
        }

        if (!boneInside.IsCracked)
        {
            return;
        }

        if (boneInside.IsPlacedOnPlate)
        {
            return;
        }

        Debug.Log(
            "[OracleBonePlate] 玩家在盘子范围内松开甲骨。",
            this
        );

        PlaceBone();
    }

    private void PlaceBone()
    {
        if (boneInside == null)
        {
            return;
        }

        OracleBoneCrack boneToPlace = boneInside;

        // 一定要先解除监听
        RemoveReleaseListener();

        boneInside = null;
        grabInteractable = null;

        // 保持玩家松手时的位置和旋转
        boneToPlace.PlaceOnPlate(plate);
    }

    private void RemoveReleaseListener()
    {
        if (grabInteractable != null)
        {
            // XRI 3.x 用 RemoveListener
            grabInteractable.selectExited.RemoveListener(OnBoneReleased);
        }
    }

    private void OnDestroy()
    {
        RemoveReleaseListener();
    }
}