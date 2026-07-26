using UnityEngine;

public class GazeTarget : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera gazeCamera;

    [Min(0.1f)]
    [SerializeField] private float maxDistance = 10f;

    [SerializeField] private LayerMask targetLayerMask = ~0;

    [SerializeField]
    private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Collide;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private GazeInteractable currentInteractable;

    private void Awake()
    {
        if (gazeCamera == null)
            gazeCamera = Camera.main;
    }

    private void Update()
    {
        if (gazeCamera == null)
            return;

        Ray ray = new Ray(
            gazeCamera.transform.position,
            gazeCamera.transform.forward
        );

        if (showDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * maxDistance,
                Color.green
            );
        }

        GazeInteractable detectedInteractable = FindInteractable(ray);

        if (detectedInteractable == currentInteractable)
            return;

        if (currentInteractable != null)
            currentInteractable.GazeExit();

        currentInteractable = detectedInteractable;

        if (currentInteractable != null)
            currentInteractable.GazeEnter();
    }

    private GazeInteractable FindInteractable(Ray ray)
    {
        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxDistance,
            targetLayerMask,
            triggerInteraction
        );

        if (!hitSomething)
            return null;


        return hit.collider.GetComponentInParent<GazeInteractable>();
    }

    private void OnDisable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.GazeExit();
            currentInteractable = null;
        }
    }
}