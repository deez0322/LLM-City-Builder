using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaceTilemapOnPlane : MonoBehaviour
{
    [SerializeField]
    private ARRaycastManager raycastManager;
    
    [SerializeField]
    private GameObject tilemapObject; // Reference to the existing tilemap object
    
    [SerializeField]
    private GameObject placementIndicator;

    private Pose placementPose;
    private bool placementPoseIsValid = false;
    public bool tilemapPlaced = false;
    

    void Start()
    {
        // Ensure the tilemap is initially deactivated
        if (tilemapObject != null)
        {
            tilemapObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!tilemapPlaced)
        {
            UpdatePlacementPose();
            UpdatePlacementIndicator();

            if (placementPoseIsValid)
            {
                // Check for touch input
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    PlaceTilemap();
                }
                
                // Check for 'X' key input
                if (Input.GetKeyDown(KeyCode.X))
                {
                    PlaceTilemap();
                }
            }
        }
    }

    void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid && !tilemapPlaced)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            // Scale the indicator to match the final size of the tilemap
            placementIndicator.transform.localScale = Vector3.one / 5f;
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    void UpdatePlacementPose()
    {
        // Ensure Camera.main references the AR Camera
        if (Camera.main == null)
        {
            Debug.LogWarning("Main Camera not found. Ensure the AR Camera is tagged as 'MainCamera'.");
            placementPoseIsValid = false;
            return;
        }

        // Get the screen center
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        
        // Perform the raycast against horizontal planes
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        raycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            // Get the pose of the first hit
            placementPose = hits[0].pose;

            // Ensure the placement is parallel to the ground
            placementPose.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void PlaceTilemap()
    {
        if (tilemapObject != null)
        {
            tilemapObject.SetActive(true);
            tilemapObject.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            
            // Scale the tilemap to one-third of its original size
            tilemapObject.transform.localScale = Vector3.one / 7f;
            
            // Make the tilemap completely static
            //MakeObjectStatic(tilemapObject);
            tilemapPlaced = true;
            placementIndicator.SetActive(false);
        }
        else
        {
            Debug.LogError("Tilemap object is not assigned!");
        }
    }

    void MakeObjectStatic(GameObject obj)
    {
        // Remove any existing Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
    }
}
