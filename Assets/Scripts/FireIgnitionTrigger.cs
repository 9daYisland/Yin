using UnityEngine;

public class FireIgnitionTrigger : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField]
    private SceneFlowController sceneFlowController;

    [Header("Torch")]
    [SerializeField]
    private string torchTag = "Torch";

    private bool hasSuccessfullyTriggered = false;

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
        if (hasSuccessfullyTriggered)
            return;

        if (sceneFlowController == null)
            return;

        // =========================================
        // Timeline1 没结束之前
        // 直接不允许任何点火
        // =========================================
        if (!sceneFlowController.CanIgniteFire)
            return;

        Transform current = other.transform;

        // Collider 可能在 Torch 子物体上
        while (current != null)
        {
            if (current.CompareTag(torchTag))
            {
                bool success =
                    sceneFlowController.TryIgniteFire();

                if (success)
                {
                    hasSuccessfullyTriggered = true;

                    Debug.Log(
                        "[FireIgnitionTrigger] " +
                        "Fire ignited by: " +
                        current.name
                    );
                }

                return;
            }

            current = current.parent;
        }
    }
}