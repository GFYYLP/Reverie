using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private RoomConfig roomConfig;
    [SerializeField] private VolumeManager volumeManager;
    [SerializeField] private RoomSpace currentRoom;
    [SerializeField] private Door doorEnter;
    [SerializeField] private Door doorExit;
    [SerializeField] private Transform booth;
    [SerializeField] private Movement player;

    [SerializeField] private float doorOffset;
    [SerializeField] private SkyManager skyboxManager;
    [SerializeField] private SurrealSpace surrealSpace;
    [SerializeField] private bool dynamicRegen = false;
    private MeshRenderer currentMesh;


    void Awake()
    {
        
    }
    
    void Start() {
        //roomConfig = RoomConfig.Default();
        AdvanceRoom();
       // nextRoom.Generate(GenerateNextConfig(roomConfig));
       
       doorExit.playerEntered += AdvanceRoom;
    }

    void Update()
    {
        if ((!dynamicRegen && Input.GetKeyDown(KeyCode.G))
           || (dynamicRegen && Time.frameCount % 3 == 0) )
        {
            AdvanceRoom();
        }
    }

    public void AdvanceRoom()
    {
        roomConfig.UpdateScores();
        volumeManager.SetRoom(roomConfig);
        currentRoom.Generate(roomConfig);
        currentMesh = currentRoom.GetComponent<MeshRenderer>();
        
        surrealSpace.AdvanceSpace();
        
        //determines world-space position of the corners of the generated mesh
        Bounds bounds = currentMesh.bounds;
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x-1f, bounds.min.y, bounds.max.z-1f);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = bounds.max;
        
        
        //place the door on bottom right corner
        doorExit.transform.position = corners[0] - new Vector3(5f + doorOffset, corners[7].y*0.5f, 5f + doorOffset);
        doorEnter.transform.position = corners[5] + new Vector3(5f - doorOffset, corners[7].y*0.5f, 5f - doorOffset);
        booth.transform.position = new Vector3(corners[5].x, corners[7].y*0.5f, corners[5].z);

        Vector3 warpPos = new Vector3(doorEnter.transform.position.x,
            booth.transform.position.y + booth.localScale.y * 0.5f,
            doorEnter.transform.position.z);

        // face the player inward toward the room center so they exit the dark door already oriented
        Vector3 toCenter = bounds.center - warpPos;
        toCenter.y = 0f;
        float inwardYaw = toCenter.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(toCenter.normalized).eulerAngles.y
            : 0f;

        player.Warp(warpPos, inwardYaw);
        skyboxManager?.OnRoomAdvance();
    }
    

    // called when player crosses room threshold
    // public void AdvanceRoom(RoomConfig incomingConfig) {
    //     prevRoom.Clear();
    //
    //     (prevRoom, currentRoom, nextRoom) 
    //         = (currentRoom, nextRoom, prevRoom);
    //
    //     // freed slot generates the room after next quietly
    //     nextRoom.Generate(incomingConfig);
    // }

    // RoomConfig GenerateNextConfig(RoomConfig last) {
    //     // placeholder: this is where grammar vector feeds in later
    //     return RoomConfig.Default();
    // }
}