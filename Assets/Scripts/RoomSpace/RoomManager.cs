using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private RoomConfig roomConfig;
    //[SerializeField] private RoomSpace prevRoom;
    [SerializeField] private RoomSpace currentRoom;
    //[SerializeField] private RoomSpace nextRoom;
    [SerializeField] private Door doorEnter;
    [SerializeField] private Door doorExit;
    [SerializeField] private Movement player;
    
    [SerializeField] private bool dynamicRegen = false;
    
    [SerializeField] private CanvasManager canvasManager;
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
        currentRoom.Generate(roomConfig);
        currentMesh = currentRoom.GetComponent<MeshRenderer>();
        
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
        doorExit.transform.position = corners[0];
        doorEnter.transform.position = corners[5];

        Vector3 prevPos = player.transform.position;
        
        //teleport player for illusion of advancing into the next one
        //player.transform.position = Vector3.zero;//doorEnter.transform.position;
        player.Warp(doorEnter.transform.position);
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