using System;
using System.Collections;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        yield return new WaitForSeconds(2f);
        _state = GameState.Playing;
    }
}