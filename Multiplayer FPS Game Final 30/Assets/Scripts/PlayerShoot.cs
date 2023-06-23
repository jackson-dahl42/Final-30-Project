// PlayerShoot class handles shooting functionality for the player character.
// It includes methods for shooting, zooming in/out, and spawning/despawning objects.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] int damage = 5; // Amount of damage the player's shots will inflict
    [SerializeField] float fireRate = 2.0f; // Rate of fire for the player's shots
    [SerializeField] KeyCode shootKey = KeyCode.Mouse0; // Key or mouse button used to shoot
    [SerializeField] LayerMask playerLayer; // Layer(s) that the player's shots can hit
    [SerializeField] LayerMask Default; // Default layer(s) that the player's shots can hit

    bool canShoot = true; // Indicates whether the player can currently shoot
    WaitForSeconds shootWait; // Used to introduce a delay between shots
    public GameObject laserToSpawn; // Reference to the laser prefab that will be spawned when the player shoots
    [HideInInspector] public GameObject spawnedLaser; // Reference to the currently spawned laser object

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            return;

        shootWait = new WaitForSeconds(fireRate); // Create a WaitForSeconds object based on the fireRate value
    }

    bool isButtonHeld = false;

    private void Update()
    {
        if (!base.IsOwner)
            return;

        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            if (canShoot)
            {
                Shoot(); // Call the Shoot method
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right click
            ZoomIn(); // Call the ZoomIn method

        if (Input.GetMouseButtonUp(1)) // Right click released
            ZoomOut(); // Call the ZoomOut method
    }

    private void ZoomIn()
    {
        Camera.main.fieldOfView = 30f; // Adjust the value to your desired zoom level
    }

    private void ZoomOut()
    {
        Camera.main.fieldOfView = 60f; // Adjust the value to your default field of view
    }

    void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, playerLayer))
        {
            if (spawnedLaser == null)
            {
                SpawnObject(laserToSpawn, transform, this, Camera.main.transform.position, hit.point); // Spawn the laser object and perform necessary actions based on the hit result
                HitPlayer(hit.transform.gameObject); // Call the HitPlayer method to deal damage to the player object
                StartCoroutine(CanShootUpdater()); // Start the CanShootUpdater coroutine to update the shooting ability
            }
        }
        else if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, Default))
        {
            if (spawnedLaser == null)
            {
                SpawnObject(laserToSpawn, transform, this, Camera.main.transform.position, hit.point); // Spawn the laser object and perform necessary actions based on the hit result
                StartCoroutine(CanShootUpdater()); // Start the CanShootUpdater coroutine to update the shooting ability
            }
        }
        else
        {
            if (spawnedLaser == null)
            {
                Vector3 laserEnd = Camera.main.transform.position + Camera.main.transform.forward * 100f;
                SpawnObject(laserToSpawn, transform, this, Camera.main.transform.position, laserEnd); // Spawn the laser object and perform necessary actions based on the hit result
                StartCoroutine(CanShootUpdater()); // Start the CanShootUpdater coroutine to update the shooting ability
            }
        }
    }

    [ServerRpc]
    public void SpawnObject(GameObject obj, Transform player, PlayerShoot script, Vector3 cameraPosition, Vector3 hitPoint)
    {
        GameObject spawned = Instantiate(obj, player.position + player.forward, Quaternion.identity); // Instantiate the laser prefab
        ServerManager.Spawn(spawned); // Synchronize the spawned object across the network using FishNet

        SetSpawnedObject(spawned, script, cameraPosition, hitPoint); // Set the positions of the line renderer in the spawned laser object
    }

    [ServerRpc]
    public void DespawnObject(GameObject obj)
    {
        ServerManager.Despawn(obj); // Despawn the laser object on the server using FishNet
    }

    IEnumerator CanShootUpdater()
    {
        canShoot = false; // Set canShoot to false

        yield return shootWait; // Wait for the shootWait duration
        yield return new WaitForSeconds(0.1f); // Introduce a small delay
        DespawnObject(spawnedLaser);
        canShoot = true; // Set canShoot to true after the delay

        // The coroutine ends here
    }

    [ObserversRpc]
    public void SetSpawnedObject(GameObject spawned, PlayerShoot script, Vector3 cameraPosition, Vector3 hitPoint)
    {
        LineRenderer lineRenderer = spawned.GetComponent<LineRenderer>(); // Get the LineRenderer component of the spawned laser object
        lineRenderer.SetPosition(0, cameraPosition); // Set the start position of the laser to the camera position
        lineRenderer.SetPosition(1, hitPoint); // Set the end position of the laser to the hit point

        script.spawnedLaser = spawned; // Assign the spawned laser object to the script's spawnedLaser variable
    }

    [ServerRpc(RequireOwnership = false)]
    void HitPlayer(GameObject playerHit)
    {
        PlayerManager.instance.DamagePlayer(playerHit.GetInstanceID(), damage, gameObject.GetInstanceID()); // Call a method to damage the player object using the PlayerManager instance
    }
}

