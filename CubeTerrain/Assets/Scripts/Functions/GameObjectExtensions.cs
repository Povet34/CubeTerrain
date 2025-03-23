using UnityEngine;

public static class GameObjectExtensions
{
    public static void ShowObject(this GameObject gameObject, bool show)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(show);
        }
    }
}