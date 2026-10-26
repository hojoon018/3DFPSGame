using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Minimap : MonoBehaviour
{
    // Todo. +/- 버튼 클릭하면...

    [SerializeField] private Camera _camera;
    [SerializeField] private Button _zoomoutButton;
    [SerializeField] private Button _zoominButton;

    private void Awake()
    {
        _zoomoutButton.onClick.AddListener(Zoomout);
        _zoominButton.onClick.AddListener(Zoomin);
    }

    private void OnDestroy()
    {
        _zoomoutButton.onClick.RemoveAllListeners();
        _zoominButton.onClick.RemoveAllListeners();
    }

    public void Zoomin()
    {
        _camera.orthographicSize -= 1;
    }

    public void Zoomout()
    {
        _camera.orthographicSize += 1;
    }
}