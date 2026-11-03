using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : class
{
    private static T _instance = null;
    public static T Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}