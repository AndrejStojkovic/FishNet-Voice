using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Example.Scened;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance
    {
        get
        {
            if(instance == null)
            {
                Debug.LogError("[ERROR] Game Manager instance not found!");
            }
            return instance;
        }
    }
    private static GameManager instance;

    [SyncVar]
    public List<PlayerController> Players = new List<PlayerController>();

    void Awake() {
        if(GameManager.Instance)
        {
            Debug.LogError("[ERROR] Game Manager instance already exists!");
            return;
        }
        instance = this;
    }

    void Start()
    {

    }

    void Update()
    {
        
    }

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        int idx = Players.Count;
        player.PlayerId = idx;
        Players.Add(player);
    }
}
