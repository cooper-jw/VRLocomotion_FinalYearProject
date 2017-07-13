using UnityEngine;
using System.Collections;

public class TeleportationLaserPointer : MonoBehaviour {
    //Settings:
    [Header("Settings:")]
    public bool active = true;
    public Color colour;
    public float lineThickness = 0.005f;
    public float maxDistance = 50.0f;
    [Space(10)]
    public GameObject holdingObject;
    public GameObject pointerObject;

    //Private variables:
    bool isActive = false;
    SteamVR_TrackedController controller;

    // Use this for initialization
    void Start () {
        holdingObject = new GameObject();
        holdingObject.transform.parent = this.transform;
        holdingObject.transform.localPosition = Vector3.zero;
        holdingObject.transform.localRotation = Quaternion.identity;

        pointerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pointerObject.transform.parent = holdingObject.transform;
        pointerObject.transform.localScale = new Vector3(lineThickness, lineThickness, 100f);
        pointerObject.transform.localPosition = new Vector3(0f, 0f, 50f);
        pointerObject.transform.localRotation = Quaternion.identity;

        BoxCollider col = pointerObject.GetComponent<BoxCollider>();
        if (col) Object.Destroy(col);

        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
        newMaterial.SetColor("_Color", colour);
        pointerObject.GetComponent<MeshRenderer>().material = newMaterial;

        controller = this.GetComponent<SteamVR_TrackedController>();
        if(!controller)
        {
            gameObject.AddComponent<SteamVR_TrackedController>();
            controller = this.GetComponent<SteamVR_TrackedController>();
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (!isActive)
        {
            isActive = true;
            this.transform.GetChild(0).gameObject.SetActive(true);
        }

        float distance = maxDistance;
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit);

        if (hasHit && hit.distance < distance)
            distance = hit.distance;

        if (controller != null && controller.triggerPressed)
            pointerObject.transform.localScale = new Vector3(lineThickness * 5f, lineThickness * 5f, distance);
        else
            pointerObject.transform.localScale = new Vector3(lineThickness, lineThickness, distance);

        pointerObject.transform.localPosition = new Vector3(0f, 0f, distance / 2f);

    }
}
