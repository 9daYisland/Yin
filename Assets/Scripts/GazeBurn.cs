using System.Collections;
using UnityEngine;

public class GazeBurn : MonoBehaviour
{
    [Header("视线")]
    public Transform vrCamera;
    public Transform targetA;
    public float rayDistance = 30f;
    public float lookTime = 10f;

    [Header("溶解")]
    public float dissolveDuration = 3f;

    // Shader Graph 里属性的 Reference 名称
    public string dissolveProperty = "_DissolveAmount";

    private float timer;
    private bool triggered;

    private void Update()
    {
        if (triggered || vrCamera == null || targetA == null)
            return;

        Ray ray = new Ray(vrCamera.position, vrCamera.forward);

        Debug.DrawRay(
            vrCamera.position,
            vrCamera.forward * rayDistance,
            Color.red
        );

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            bool lookingAtA =
                hit.transform == targetA ||
                hit.transform.IsChildOf(targetA) ||
                targetA.IsChildOf(hit.transform);

            if (lookingAtA)
            {
                timer += Time.deltaTime;
                Debug.Log("正在看 A：" + timer);

                if (timer >= lookTime)
                {
                    triggered = true;
                    Debug.Log("注视完成，开始 Burn");
                    StartCoroutine(BurnAllObjects());
                }
            }
            else
            {
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    private IEnumerator BurnAllObjects()
    {
        GameObject[] burnObjects = GameObject.FindGameObjectsWithTag("Burn");

        Debug.Log("找到 Burn 物体数量：" + burnObjects.Length);

        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Clamp01(elapsed / dissolveDuration);

            foreach (GameObject burnObject in burnObjects)
            {
                Renderer[] renderers =
                    burnObject.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer rend in renderers)
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat.HasProperty(dissolveProperty))
                        {
                            mat.SetFloat(dissolveProperty, value);
                        }
                        else
                        {
                            Debug.LogWarning(
                                mat.name +
                                " 没有属性：" +
                                dissolveProperty
                            );
                        }
                    }
                }
            }

            yield return null;
        }
    }
}