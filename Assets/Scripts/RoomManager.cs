using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public RoomConfig roomConfig;
    public RoomSpace prevRoom;
    public RoomSpace currentRoom;
    public RoomSpace nextRoom;

    public bool dynamicRegen = false;
    
    void Start() {
        //roomConfig = RoomConfig.Default();
        currentRoom.Generate(roomConfig);
       // nextRoom.Generate(GenerateNextConfig(roomConfig));
    }

    void Update()
    {
        if (!dynamicRegen && Input.GetKeyDown(KeyCode.G))
        {
            currentRoom.Generate(roomConfig);
        }
        if (dynamicRegen && Time.frameCount % 3 == 0)
        {
            currentRoom.Generate(roomConfig);
        }
    }

    // called when player crosses room threshold
    public void AdvanceRoom(RoomConfig incomingConfig) {
        prevRoom.Clear();

        (prevRoom, currentRoom, nextRoom) 
            = (currentRoom, nextRoom, prevRoom);

        // freed slot generates the room after next quietly
        nextRoom.Generate(incomingConfig);
    }

    // RoomConfig GenerateNextConfig(RoomConfig last) {
    //     // placeholder — this is where grammar vector feeds in later
    //     return RoomConfig.Default();
    // }
}