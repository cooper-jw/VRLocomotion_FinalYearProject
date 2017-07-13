using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class GrabAndDrag : MonoBehaviour {
    [Header("Simulation Sickness Settings:")]
    [SerializeField] bool fovLimiterEnabled = true;
    [SerializeField] float fovLimitSize = 0.65f;
    [SerializeField] float fovFadeTime = 0.3f;

    //Movement variables:
    bool moving = false;
    Vector3 initalControllerPosition, initalCameraPosition;
    Vector3 cameraDelta, controllerDelta;
    float initalCameraRotation, initalControllerRotation;
    float cameraRotationDelta, controllerRotationDelta;
    float cameraRotation;
    bool isGrip = false;

    //Controller and camera variables:
    [Space(10)]
    [SerializeField] SteamVR_TrackedObject controller;
    [SerializeField] GameObject cameraRig;
    SteamVR_Controller.Device device;
    //Fov limiter variables:
    VignetteAndChromaticAberration fovLimiter;
    float currentFovLimit = 0.0f;
    bool limitingFov = false;
    float fovTimer = 0.0f;


    // Use this for initialization
    void Awake () {
        cameraDelta = Vector3.zero;
        controller = GetComponent<SteamVR_TrackedObject>();
        if(cameraRig != null)
            fovLimiter = cameraRig.GetComponentInChildren<VignetteAndChromaticAberration>();
	}
	
	// Update is called once per frame
	void Update () {
	    device = SteamVR_Controller.Input((int)controller.index);

        if (device.GetTouchDown(SteamVR_Controller.ButtonMask.Grip))
            isGrip = true;
        else if (device.GetTouchUp(SteamVR_Controller.ButtonMask.Grip))
            isGrip = false;

        if(isGrip) {
            if(!moving) {
                //Set initial controller values:
                initalControllerPosition = transform.position;
                initalControllerRotation = transform.eulerAngles.y;
                //Set initial camera values:
                initalCameraPosition = cameraRig.transform.position;
                initalCameraRotation = cameraRig.transform.eulerAngles.y;
                //Reset delta values:
                cameraRotationDelta = 0.0f;
                cameraDelta = Vector3.zero;
                //Set moving:
                moving = true;
                limitingFov = true;
            }

            //Calculate controller delta:
            controllerDelta.x = transform.position.x - initalControllerPosition.x - cameraDelta.x;
            controllerDelta.z = transform.position.z - initalControllerPosition.z - cameraDelta.z;
            controllerRotationDelta = transform.eulerAngles.y - initalControllerRotation - cameraRotationDelta;

            //Update camera values:
            cameraRig.transform.position = initalCameraPosition - controllerDelta;
            cameraRig.transform.rotation = Quaternion.Euler(0.0f, initalCameraRotation - controllerRotationDelta, 0.0f);

            //Update delta values:
            cameraDelta = cameraRig.transform.position - initalCameraPosition;
            cameraRotationDelta = cameraRig.transform.eulerAngles.y - initalCameraRotation;
        } else {
            moving = false;
            limitingFov = false;
        }

        if (fovLimiterEnabled) CalculateFovLimit();
	}

    /*
        Calculates current fov limit using settings from editor
        Then applies using image effects
    */
    void CalculateFovLimit()
    {
        if (fovLimiter == null) return;

        if(limitingFov && fovTimer < fovFadeTime && currentFovLimit != fovLimitSize)
        {
            fovTimer += Time.deltaTime;
            float perc = remap(fovTimer, 0.0f, fovFadeTime, 0.0f, 1.0f);
            currentFovLimit = Mathf.Lerp(0.0f, fovLimitSize, perc);
            fovLimiter.intensity = currentFovLimit;
        } else if(limitingFov && fovTimer >= fovFadeTime)
        {
            fovTimer = 0.0f;
            currentFovLimit = fovLimitSize;
            fovLimiter.intensity = fovLimitSize;
        } else if(!limitingFov && currentFovLimit > 0.0f && fovTimer < fovFadeTime)
        {
            fovTimer += Time.deltaTime;
            float perc = remap(fovTimer, 0.0f, fovFadeTime, 0.0f, 1.0f);
            currentFovLimit = Mathf.Lerp(fovLimitSize, 0.0f, perc);
            fovLimiter.intensity = currentFovLimit;
        } else if(!limitingFov && currentFovLimit != 0.0f)
        {
            fovTimer = 0.0f;
            currentFovLimit = 0.0f;
            fovLimiter.intensity = 0.0f;
        }
    }

    /*
        Remaps a value (value) from one range (from1-from2) to another range (to1 - to2)
    */
    public static float remap(float value, float from1, float from2, float to1, float to2)
    {
        return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
    }
}
