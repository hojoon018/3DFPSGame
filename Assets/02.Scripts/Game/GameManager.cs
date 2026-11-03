using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    Pause,
    GameOver
}

public class GameManager : SingletonBehaviour<GameManager>
{
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    private Player _player;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private GameObject _optionPopup;

    private void Awake()
    {
        _player = FindAnyObjectByType<Player>();
    }

    private void Start()
    {
        _optionPopup.SetActive(false);

        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"유니티 흐른 시간: {Time.deltaTime}");
            Debug.Log($"유니티 흐른 시간: {Time.unscaledDeltaTime}");

            if (_state == GameState.Playing)
            {
                _state = GameState.Pause;
                Time.timeScale = 0; // 유니티에서 흐르는 게임 시간
                _optionPopup.SetActive(true);
                CursorManager.Instance.UnlockCursor();
            }
            else if (_state == GameState.Pause)
            {
                _state = GameState.Playing;
                Time.timeScale = 1;
                _optionPopup.SetActive(false);
                CursorManager.Instance.LockCursor();
            }
        }
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;

        _player.GetComponentInChildren<Animator>().SetFloat("MoveMotion", 0f);

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임 오버";
        _stateText.color = Color.red;
    }
}