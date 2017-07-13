using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using Valve.VR;

public class DeviceManager : MonoBehaviour {
    public static DeviceManager _instance = null;

    public enum Devices {
        Headset,
        LeftController,
        RightController,
    }

    public enum Controllers
    {
        Left,
        Right,
    }

    [SerializeField] GameObject rightControllerGO, leftControllerGO;
    [SerializeField] GameObject headDeviceGO;
    [SerializeField] GameObject playSpace;
    SteamVR_TrackedObject leftTrackedObject, rightTrackedObject;
    SteamVR_Controller.Device leftController, rightController;

    void Awake() {
        if (_instance)
        {
            Destroy(this.gameObject);
        }
        else _instance = this;

        if(playSpace == null)
        {
            playSpace = GameObject.FindGameObjectWithTag("CameraRig");
            if (playSpace == null) Debug.LogError("Playspace not found");
        }
    }

    void Start()
    {
        StartCoroutine(WaitControllerConnected());
    }

    IEnumerator WaitControllerConnected()
    {
        while (leftTrackedObject == null || rightTrackedObject == null || leftController == null || rightController == null)
        {
            if (leftTrackedObject == null)
                leftTrackedObject = leftControllerGO.GetComponent<SteamVR_TrackedObject>();

            if (leftTrackedObject != null)
                if (leftTrackedObject.isValid)
                    leftController = SteamVR_Controller.Input((int)leftTrackedObject.index);


            if (rightTrackedObject == null)
                rightTrackedObject = rightControllerGO.GetComponent<SteamVR_TrackedObject>();

            if (rightTrackedObject != null)
                if (rightTrackedObject.isValid)
                    rightController = SteamVR_Controller.Input((int)rightTrackedObject.index);

            yield return new WaitForEndOfFrame();
        }
    }

    public Transform GetDeviceTransform(Devices device) {
        switch (device) {
            case Devices.Headset:
                return GetHeadsetTransform();
            case Devices.LeftController:
                return GetControllerLeftHandObject().transform;
            case Devices.RightController:
                return GetControllerRightHandObject().transform;
        }
        return null;
    }

    public Vector3 GetDevicePosition(Devices device) {
        switch (device) {
            case Devices.Headset:
                return GetHeadsetPosition();
            case Devices.LeftController:
                return GetControllerLeftHandObject().transform.position;
            case Devices.RightController:
                return GetControllerRightHandObject().transform.position;
        }
        return Vector3.zero;
    }

    public Quaternion GetDeviceRotation(Devices device) {
        switch (device) {
            case Devices.Headset:
                return GetHeadsetRotation();
            case Devices.LeftController:
                return GetControllerLeftHandObject().transform.rotation;
            case Devices.RightController:
                return GetControllerRightHandObject().transform.rotation;
        }
        return Quaternion.identity;
    }

    public Vector3 GetDeviceEulerAngles(Devices device) {
        switch (device) {
            case Devices.Headset:
                return GetHeadsetEulerAngles();
            case Devices.LeftController:
                return GetControllerLeftHandObject().transform.eulerAngles;
            case Devices.RightController:
                return GetControllerRightHandObject().transform.eulerAngles;
        }
        return Vector3.zero;
    }

    public Transform GetHeadsetTransform()
    {
        return headDeviceGO.transform;
    }

    public Vector3 GetHeadsetPosition()
    {
        return headDeviceGO.transform.position;
    }

    public Quaternion GetHeadsetRotation()
    {
        return headDeviceGO.transform.rotation;
    }

    public Vector3 GetHeadsetEulerAngles()
    {
        return headDeviceGO.transform.eulerAngles;
    }

    public SteamVR_Controller.Device GetControllerLeftHand()
    {
        return leftController;
    }

    public SteamVR_Controller.Device GetControllerRightHand()
    {
        return rightController;
    }

    public GameObject GetControllerLeftHandObject()
    {
        return leftControllerGO;
    }

    public GameObject GetControllerRightHandObject()
    {
        return rightControllerGO;
    }

    public Transform GetPlaySpaceTransform()
    {
        return playSpace.transform;
    }
}
