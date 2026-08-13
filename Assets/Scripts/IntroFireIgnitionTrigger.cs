using UnityEngine;

public class IntroFireIgnitionTrigger : MonoBehaviour
{
    [Header("Fire")]
    [Tooltip("要被点燃的火把上的 FireController")]
    [SerializeField] private FireController fireController;

    [Header("Candle")]
    [Tooltip("蜡烛根物体使用的 Tag")]
    [SerializeField] private string candleTag = "Candle";

    private bool hasIgnited = false;


    private void OnTriggerEnter(Collider other)
    {
        TryIgnite(other);
    }


    private void OnTriggerStay(Collider other)
    {
        TryIgnite(other);
    }


    private void TryIgnite(Collider other)
    {
        // 已经点燃过，不再重复触发
        if (hasIgnited)
            return;

        // 没绑定 FireController
        if (fireController == null)
        {
            Debug.LogWarning(
                "[IntroFireIgnitionTrigger] FireController 没有绑定！"
            );

            return;
        }


        // 从碰撞到的 Collider 开始
        // 一直往父物体寻找 Candle Tag
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(candleTag))
            {
                Debug.Log(
                    "[IntroFireIgnitionTrigger] 检测到蜡烛：" +
                    current.name
                );

                hasIgnited = true;

                // 点燃火把
                fireController.FadeIn();

                return;
            }

            current = current.parent;
        }
    }
}