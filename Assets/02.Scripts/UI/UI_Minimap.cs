using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _camera;
    [SerializeField] private Button _zoomoutButton;
    [SerializeField] private Button _zoominButton;

    private void Awake()
    {
        _zoomoutButton.onClick.AddListener(_camera.ZoomOut);
        _zoominButton.onClick.AddListener(_camera.ZoomIn);
    }

    private void OnDestroy()
    {
        _zoomoutButton.onClick.RemoveListener(_camera.ZoomOut);
        _zoominButton.onClick.RemoveListener(_camera.ZoomIn);
    }
}