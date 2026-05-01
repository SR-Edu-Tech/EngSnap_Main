using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_Singleton<T> : MonoBehaviour where T : MonoBehaviour {


    public static T Instance { get; private set; }


    protected virtual void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(Instance);
        } else {
            Instance = this as T;
        }
    }

    
}
