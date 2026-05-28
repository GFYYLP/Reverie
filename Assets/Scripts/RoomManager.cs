using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public RoomSpace prevRoom;
    public RoomSpace currentRoom;
    public RoomSpace nextRoom;

    void Start() {
        currentRoom.Generate(RoomConfig.Default());
        nextRoom.Generate(GenerateNextConfig(RoomConfig.Default()));
    }

    // called when player crosses room threshold
    public void AdvanceRoom(RoomConfig incomingConfig) {
        prevRoom.Clear();

        (prevRoom, currentRoom, nextRoom) 
            = (currentRoom, nextRoom, prevRoom);

        // freed slot generates the room after next quietly
        nextRoom.Generate(incomingConfig);
    }

    RoomConfig GenerateNextConfig(RoomConfig last) {
        // placeholder — this is where grammar vector feeds in later
        return RoomConfig.Default();
    }
}