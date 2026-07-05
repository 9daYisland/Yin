using System.Collections;
using UnityEngine;

public class Burn: MonoBehaviour
{
	[SerializeField] private string targetTag = "RevealObject";
	[SerializeField] private string materialPropertyName = "_DissolveAmount";
	[SerializeField] private float duration = 2f;

	private Renderer[] targetRenderers;
	private bool isPlaying;

	private void Start()
	{
		GameObject[] objects = GameObject.FindGameObjectsWithTag(targetTag);

		var renderers = new System.Collections.Generic.List<Renderer>();

		foreach (GameObject obj in objects)
		{
			renderers.AddRange(obj.GetComponentsInChildren<Renderer>());
		}

		targetRenderers = renderers.ToArray();
	}

	private void OnMouseDown()
	{
		if (!isPlaying)
		{
			StartCoroutine(ChangeMaterialValue());
		}
	}

	private IEnumerator ChangeMaterialValue()
	{
		isPlaying = true;

		float timer = 0f;

		while (timer < duration)
		{
			timer += Time.deltaTime;
			float value = Mathf.Clamp01(timer / duration);

			foreach (Renderer r in targetRenderers)
			{
				if (r != null)
					r.material.SetFloat(materialPropertyName, value);
			}

			yield return null;
		}

		isPlaying = false;
	}
}