using UnityEngine;

public class LanternIgniteTrigger : MonoBehaviour
{
    [Header("天灯控制器")]
    [SerializeField]
    private LanternFlyController lanternController;

    [Header("能够点火的物体 Tag")]
    [SerializeField]
    private string torchTag = "Torch";


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "[LanternIgniteTrigger] Enter: " +
            other.name
        );

        // 检查碰到的物体，以及它的父物体
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(torchTag))
            {
                Debug.Log(
                    "[LanternIgniteTrigger] Torch detected!"
                );

                if (lanternController != null)
                {
                    lanternController.IgniteLantern();
                }

                return;
            }

            current = current.parent;
        }
    }
}