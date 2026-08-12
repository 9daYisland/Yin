using System.Collections;
using TMPro;
using UnityEngine;

public class PickupInfoUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;

    [Header("Content")]
    [TextArea(3, 10)]
    [SerializeField] private string fullText;

    [Header("Typing")]
    [SerializeField] private float secondsPerCharacter = 0.05f;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (infoText != null)
            infoText.text = "";
    }

    public void ShowInfo()
    {
        if (infoPanel == null || infoText == null)
        {
            Debug.LogWarning($"{name} 的 PickupInfoUI 没有挂完整。");
            return;
        }

        infoPanel.SetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

    public void HideInfo()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (infoText != null)
            infoText.text = "";

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private IEnumerator TypeText()
    {
        infoText.text = "";

        foreach (char character in fullText)
        {
            infoText.text += character;

            // 标点符号稍微停顿久一点
            float delay = secondsPerCharacter;

            if (character == '，' ||
                character == '。' ||
                character == '！' ||
                character == '？' ||
                character == ',' ||
                character == '.' ||
                character == '!' ||
                character == '?')
            {
                delay *= 4f;
            }

            yield return new WaitForSeconds(delay);
        }

        typingCoroutine = null;
    }
}