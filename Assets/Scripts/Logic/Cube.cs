using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer))]
public class Cube : MonoBehaviour {
    private bool isActive = false;

    [SerializeField]
    private Material activeMat, inactiveMat;
    private MeshRenderer meshRender;

	// Use this for initialization
	void Start () {
        meshRender = this.GetComponent<MeshRenderer>();
	}

    public void Toggle()
    {
        Toggle(!isActive);
    }

    public void Toggle(bool active)
    {
        if (active)
            meshRender.material = activeMat;
        else
            meshRender.material = inactiveMat;

        isActive = active;

        if (GameController._instance)
            GameController._instance.CheckCubes();
    }

    //Getter method for isActive:
    public bool GetActive() { return isActive; }
}
