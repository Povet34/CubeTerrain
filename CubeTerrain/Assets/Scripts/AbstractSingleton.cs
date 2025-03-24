using UnityEngine;
using System.Collections;
using Colorverse;

public class AbstractSingleton<Type> : MonoBehaviour 
{
    public const string APP_OBJECT_NAME = "Application";
    private static Type _instance;
	public static Type Instance
	{
		get
		{  
			if ( _instance == null )
			{
				//Debug.Log ("AbstractSingleton " + typeof(Type) + "," + UnityEngine.StackTraceUtility.ExtractStackTrace());
				_instance = (Type)(object)FindObjectOfType(typeof(Type));
				if ( _instance == null )
				{
					GameObject application = GameObject.Find(APP_OBJECT_NAME);
					if (application != null)
						_instance = (Type)(object)application.AddComponent(typeof(Type));
					else {
						application = new GameObject(APP_OBJECT_NAME);
						_instance = (Type)(object)application.AddComponent(typeof(Type));
						//application.AddComponent<DontDestroy>();
					}
				}
				((AbstractSingleton<Type>)(object)_instance)._Init();
			}
			
			return _instance;
		}
	}

	public static bool HasInstance() 
	{
		return _instance != null;
	}
	
	public virtual void _Init() 
	{
	}
}

public class AbstractGlobalInstance<Type> : MonoBehaviour 
{
	public static Type Get() 
	{
		return (Type)(object)FindObjectOfType(typeof(Type));
	}
}