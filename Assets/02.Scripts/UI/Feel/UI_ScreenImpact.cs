using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 흔들림과 플래시로 타격감을 준다.
// 흔들 대상은 팝업/페이드를 제외한 화면 요소를 묶은 루트 RectTransform이다.
public class UI_ScreenImpact : MonoBehaviour
{
    [SerializeField] private RectTransform _shakeRoot;
    [SerializeField] private Image _flash;

    private Coroutine _shakeCoroutine;
    private Coroutine _flashCoroutine;

    public void Shake(float duration, float strength)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }

        _shakeCoroutine = StartCoroutine(Shake_Coroutine(duration, strength));
    }

    public void Flash(Color color, float duration)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(Flash_Coroutine(color, duration));
    }

    private IEnumerator Shake_Coroutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 시간이 지날수록 약해지는 흔들림
            float falloff = 1f - elapsed / duration;
            _shakeRoot.anchoredPosition = Random.insideUnitCircle * strength * falloff * falloff;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _shakeRoot.anchoredPosition = Vector2.zero;
        _shakeCoroutine = null;
    }

    private IEnumerator Flash_Coroutine(Color color, float duration)
    {
        _flash.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            Color current = color;
            current.a = color.a * (1f - elapsed / duration);
            _flash.color = current;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _flash.gameObject.SetActive(false);
        _flashCoroutine = null;
    }
}
