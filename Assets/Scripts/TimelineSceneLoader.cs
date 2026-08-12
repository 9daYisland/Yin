using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineSceneLoader : MonoBehaviour
{
    [Header("需要监听的 Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Timeline 结束后进入的场景")]
    [SerializeField] private string nextSceneName = "tomb1";

    private bool hasLoadedScene;

    private void OnEnable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped += OnTimelineStopped;
        }
    }

    private void OnDisable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (hasLoadedScene)
        {
            return;
        }

        hasLoadedScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}