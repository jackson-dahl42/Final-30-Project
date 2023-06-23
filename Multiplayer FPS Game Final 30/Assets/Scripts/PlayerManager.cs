using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager instance;
    private void Awake()
    {
        instance = this;
    }

    // Dictionary to store players with their IDs
    public Dictionary<int, Player> players = new Dictionary<int, Player>();
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    // Method to damage a player
    public void DamagePlayer(int playerID, int damage, int attackerID)
    {
        if (!base.IsServer)
            return;

        players[playerID].health -= damage;
        print("Player " + playerID.ToString() + " health is " + players[playerID].health);

        if (players[playerID].health <= 0)
        {
            PlayerKilled(playerID, attackerID);
        }

        // Update the health on the server
        UpdateHealth(this, playerID, players[playerID].health);
    }

    // Method to handle a player being killed
    void PlayerKilled(int playerID, int attackerID)
    {
        print("Player " + playerID.ToString() + " was killed by " + attackerID.ToString());
        players[playerID].deaths++;
        players[playerID].health = 100;
        players[attackerID].kills++;

        // Update the deaths and kills on the server
        UpdateDeaths(this, playerID, players[playerID].deaths);
        UpdateKills(this, attackerID, players[attackerID].kills);

        RespawnPlayer(players[playerID].connection, players[playerID].playerObject, Random.Range(0, spawnPoints.Count));
    }

    // RPC method to respawn a player on the client
    [TargetRpc]
    void RespawnPlayer(NetworkConnection conn, GameObject player, int spawn)
    {
        player.transform.position = spawnPoints[spawn].position;
    }

    // Player class to store player information
    public class Player 
    {
        [SyncVar]
        public int health = 100;

        [SyncVar]
        public int kills = 0;

        [SyncVar]
        public int deaths = 0;

        public GameObject playerObject;
        public NetworkConnection connection;
    }

    // Server RPC method to update the health of a player
    [ServerRpc]
    public void UpdateHealth(PlayerManager script, int playerID, int newHealth)
    {
        players[playerID].health = newHealth;
    }

    // Server RPC method to update the deaths of a player
    [ServerRpc]
    public void UpdateDeaths(PlayerManager script, int playerID, int newDeaths)
    {
        players[playerID].deaths = newDeaths;
    }

    // Server RPC method to update the kills of a player
    [ServerRpc]
    public void UpdateKills(PlayerManager script, int playerID, int newKills)
    {
        players[playerID].kills = newKills;
    }
}
