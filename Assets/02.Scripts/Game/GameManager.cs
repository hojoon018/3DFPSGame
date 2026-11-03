using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Pause,
    GameOver
}

// 게임 상태를 관리한다. 씬이 바뀌어도 유지되므로 씬에 있는 UI를 직접 참조하지 않고,
// 상태가 바뀔 때 OnStateChanged 이벤트로 알린다. (UI는 UI_GameState가 구독해서 갱신)
public class GameManager : SingletonBehaviour<GameManager>
{
    private const float ReadyDuration = 1.5f;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    public event Action<GameState> OnStateChanged;

    // 씬에 속한 오브젝트라 씬이 다시 로드될 때마다 새로 찾는다.
    private Player _player;

    private void Start()
    {
        // 씬 재시작 시 새로 생긴 중복 인스턴스는 Awake에서 파괴되므로 초기화하지 않는다.
        if (!ReferenceEquals(Instance, this)) return;

        // 첫 씬은 여기서 시작하고, 이후 씬이 다시 로드될 때마다 OnSceneLoaded에서 다시 시작한다.
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartGame();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"유니티 흐른 시간: {Time.deltaTime}");
            Debug.Log($"유니티 흐른 시간: {Time.unscaledDeltaTime}");

            if (_state == GameState.Playing)
            {
                Pause();
            }
            else if (_state == GameState.Pause)
            {
                Resume();
            }
        }
    }

    private void StartGame()
    {
        _player = FindAnyObjectByType<Player>();

        StopAllCoroutines();
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        ChangeState(GameState.Ready);
        yield return new WaitForSeconds(ReadyDuration);
        ChangeState(GameState.Playing);
    }

    private void ChangeState(GameState state)
    {
        _state = state;
        OnStateChanged?.Invoke(_state);
    }

    public void GameOver()
    {
        _player.GetComponentInChildren<Animator>().SetFloat("MoveMotion", 0f);

        ChangeState(GameState.GameOver);
    }

    // 일시정지
    public void Pause()
    {
        Time.timeScale = 0; // 유니티에서 흐르는 게임 시간
        CursorManager.Instance.UnlockCursor();
        ChangeState(GameState.Pause);
    }

    // 계속하기
    public void Resume()
    {
        Time.timeScale = 1f;
        CursorManager.Instance.LockCursor();
        ChangeState(GameState.Playing);
    }

    // 다시하기
    public void Restart()
    {
        Time.timeScale = 1f;
        // CursorManager는 씬이 바뀌어도 유지되므로 일시정지 때 풀린 커서를 다시 잠근다.
        CursorManager.Instance.LockCursor();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 게임 종료
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
