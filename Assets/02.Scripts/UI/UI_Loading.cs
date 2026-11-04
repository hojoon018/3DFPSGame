using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class UI_Loading : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // 비동기 씬 로드
        // 반환 : 씬의 비동기 진행사항을 확인할 수 있는 객체
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName);
        // 수많은 객체의 생성을 쪼개서 하나하나 생성할 준비를
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }
}