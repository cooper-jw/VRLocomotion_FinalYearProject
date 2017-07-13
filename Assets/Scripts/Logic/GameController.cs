using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour {
    public static GameController _instance;

    [SerializeField]
    Cube[] cubes;

    [SerializeField]
    bool completed = false;

    void Awake() {
        if (_instance == null)
            _instance = this;
        else {
            Destroy(_instance.gameObject);
            _instance = this;
        }
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public bool CheckCubes()
    {
        bool isCompleted = true;

        for (int i = 0; i < cubes.Length; ++i)
            if (!cubes[i].GetActive()) isCompleted = false;

        completed = isCompleted;

        return isCompleted;
    }

}
