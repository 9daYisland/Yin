using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
public class Breakable : MonoBehaviour
{
	[SerializeField] private GameObject intactBone;
	[SerializeField] private GameObject brokenBone;

	[Header("Transition")]
	[SerializeField] private float delayBeforeSwitch = 0.35f;
	[SerializeField] private float shakeAmount = 0.01f;
	[SerializeField] private float shakeSpeed = 45f;

	[Header("Drag")]
	[SerializeField] private float dragDepth = 5f;

	private BoxCollider bc;
	private bool isBreaking;
	private bool isDragging;
	private Vector3 originalLocalPosition;
	private Vector3 dragOffset;

	private void Awake()
	{
		intactBone.SetActive(true);
		brokenBone.SetActive(false);

		bc = GetComponent<BoxCollider>();
		originalLocalPosition = intactBone.transform.localPosition;
	}

	private void OnMouseDown()
	{
		if (isBreaking) return;

		isDragging = true;

		Vector3 mouseWorldPos = GetMouseWorldPosition();
		dragOffset = transform.position - mouseWorldPos;
	}

	private void OnMouseDrag()
	{
		if (!isDragging || isBreaking) return;

		transform.position = GetMouseWorldPosition() + dragOffset;
	}

	private void OnMouseUp()
	{
		isDragging = false;
	}

	private Vector3 GetMouseWorldPosition()
	{
		Vector3 mousePos = Mouse.current.position.ReadValue();
		mousePos.z = dragDepth;
		return Camera.main.ScreenToWorldPoint(mousePos);
	}

	private void OnTriggerEnter(Collider other)
	{
		Debug.Log("碰到了：" + other.name + "，Tag是：" + other.tag);

		if (isBreaking) return;

		if (other.CompareTag("Fire"))
		{
			Debug.Log("碰到火焰，开始碎裂");
			StartCoroutine(BreakAnimation());
		}
	}

	private IEnumerator BreakAnimation()
	{
		isBreaking = true;
		isDragging = false;

		if (bc != null)
		{
			bc.enabled = false;
		}

		float timer = 0f;

		while (timer < delayBeforeSwitch)
		{
			timer += Time.deltaTime;

			float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
			float shakeY = Mathf.Cos(Time.time * shakeSpeed * 0.8f) * shakeAmount;

			intactBone.transform.localPosition = originalLocalPosition + new Vector3(shakeX, shakeY, 0f);

			yield return null;
		}

		intactBone.transform.localPosition = originalLocalPosition;

		intactBone.SetActive(false);
		brokenBone.SetActive(true);
	}
}