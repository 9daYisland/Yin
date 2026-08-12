using System.Collections;
using UnityEngine;

public class DissolveTest : MonoBehaviour
{
    [SerializeField] private SceneDissolveController dissolveController;
    [SerializeField] private float delay = 2f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delay);
        dissolveController.PlayDissolve();
    }
}