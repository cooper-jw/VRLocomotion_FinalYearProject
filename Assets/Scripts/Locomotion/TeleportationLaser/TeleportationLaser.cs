using UnityEngine;
using System.Collections;

/*

*/
public class TeleportationLaser : MonoBehaviour {
    //Teleportation modes:
    public enum TeleportMode
    {
        Collider,
        ZeroHeight,
    }

    //Returns player transform based upon SteamVR_Camera transform
    Transform playerOrigin {
        get
        {
            SteamVR_Camera vrCamera = SteamVR_Render.Top();
            return (vrCamera != null) ? vrCamera.origin : null;
        }
    }

    //Settings:
    [Header("Settings:")]
    public bool enableTeleport = true;
    public bool teleportOnClick = false;
    public bool compensateRoomPosition = true;
    public TeleportMode teleportMode = TeleportMode.ZeroHeight;


    private Transform headset;
    private Transform playSpace;
    private SteamVR_TrackedController controller;

    // Use this for initialization
    void Start()
    {
        if (DeviceManager._instance)
        {
            headset = DeviceManager._instance.GetHeadsetTransform();
            playSpace = DeviceManager._instance.GetPlaySpaceTransform();
            controller = this.GetComponent<SteamVR_TrackedController>();

            if (controller == null)
            {
                gameObject.AddComponent<SteamVR_TrackedController>();
                controller = this.GetComponent<SteamVR_TrackedController>();
            }

            controller.TriggerClicked += new ClickedEventHandler(OnClickEvent);
        }
        else Debug.LogError("FATAL: DeviceManager instance is not initialised!");
    }
    
    void OnClickEvent(object sender, ClickedEventArgs e)
    {
        if(teleportOnClick && enableTeleport)
        {
            //Get transform of player:
            Transform t = playerOrigin;
            if (!t) return;

            //Get Y position of player:
            float playerY = t.position.y;

            //Create ray from controller along ground plane in the direction of controller:
            Plane ground = new Plane(Vector3.up, -playerY);
            Ray ray = new Ray(this.transform.position, transform.forward);

            //Variables to store validation checks:
            bool targetGround = false;
            float distance = 0.0f;

            //Use ray to raycast teleport position based on teleport mode:
            if(teleportMode == TeleportMode.Collider)
            {
                RaycastHit hit;
                targetGround = Physics.Raycast(ray, out hit);
                distance = hit.distance;
            } else if(teleportMode == TeleportMode.ZeroHeight)
            {
                targetGround = ground.Raycast(ray, out distance);
            }

            //If teleport position is on the ground:
            if(targetGround)
            {
                //Get camera position on groud relative to world space:
                Vector3 cameraGroundPosition = new Vector3(SteamVR_Render.Top().head.position.x, playerY, SteamVR_Render.Top().head.position.z);
                //Move player to new position:
                t.position = t.position + (ray.origin + (ray.direction * distance)) - cameraGroundPosition;
            }
        }
    }

}
