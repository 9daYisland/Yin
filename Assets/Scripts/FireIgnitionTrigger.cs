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
        TryTrigger(other);
    }


    private void OnTriggerStay(Collider other)
    {
        // 很重要：
        // 如果 Torch 在 Timeline1 结束前已经进入 Trigger，
        // Timeline1 结束后仍然可以检测到它。
        TryTrigger(other);
    }


    private void TryTrigger(Collider other)
    {
        if (hasSuccessfullyTriggered)
            return;

        if (sceneFlowController == null)
            return;


        Transform current = other.transform;


        // Collider 可能在 Torch 的子物体上，
        // 所以一路向父节点寻找 Torch Tag。
        while (current != null)
        {
            if (current.CompareTag(torchTag))
            {
                // 只有 SceneFlow 真正接受了这次点火，
                // 才永久锁定 Trigger。
                bool success =
                    sceneFlowController.TryIgniteFire();


                if (success)
                {
                    hasSuccessfullyTriggered = true;

                    Debug.Log(
                        "[FireIgnitionTrigger] " +
                        "Fire successfully ignited by: " +
                        current.name
                    );
                }

                return;
            }


            current = current.parent;
        }
    }
}