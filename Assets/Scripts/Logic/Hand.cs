using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Hand : MonoBehaviour {

    void Start()
    {

    }

	void OnTriggerEnter(Collider other)
    {
        if(other.tag.Equals("TargetCube"))
            other.gameObject.GetComponent<Cube>().Toggle(true);
    }
}
