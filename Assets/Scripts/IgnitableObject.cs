using UnityEngine;

public class IgnitableObject : MonoBehaviour
{
    [Header("点燃后出现的对象")]
    [SerializeField] private GameObject[] fireObjects;


    [Header("点燃设置")]
    [SerializeField] private string torchTag = "Torch";
    [SerializeField] private bool canIgniteOnlyOnce = true;

    private bool isIgnited;

    private void Start()
    {
        // 游戏开始时隐藏所有燃烧效果
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

    public void Ignite()
    {
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

    }
}