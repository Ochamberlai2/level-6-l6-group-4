using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//http://www.unitygeek.com/unity_c_singleton/
/// <summary>
/// Inherit all singletons from this class
/// </summary

public class SingletonClass<T> : MonoBehaviour where T : Component {

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null) {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }
	public virtual void Awake()
    {
        if(instance == null)
        {
            instance = this as T;
            
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
