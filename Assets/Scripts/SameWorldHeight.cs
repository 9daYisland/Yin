using UnityEngine;

public class SameWorldHeight : MonoBehaviour
{
    public Transform[] objects;
    public float worldY = 2f;

    [ContextMenu("Set Same Height")]
    void SetSameHeight()
    {
        foreach (Transform obj in objects)
        {
            Vector3 pos = obj.position;
            pos.y = worldY;
            obj.position = pos;
        }
    }
}