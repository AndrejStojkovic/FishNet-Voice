using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance
    {
        get
        {
            if(instance == null)
            {
                Debug.LogError("Game Manager instance not found!");
            }
            return instance;
        }
    }
    private static GameManager instance;

    public readonly SyncList<Player> Players = new SyncList<Player>();

    void Awake() {
        if(GameManager.Instance)
        {
            Debug.LogError("Game Manager instance already exists!");
            return;
        }
        instance = this;
    }


    [Server]
    public void RegisterPlayer(Player player)
    {
        int idx = Players.Count;
        player.PlayerId.Value = idx;
        Players.Add(player);
    }
}
