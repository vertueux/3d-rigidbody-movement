using UnityEngine;

public class GrapplingGun : MonoBehaviour 
{
    
    private Vector3 grapplePoint;
    public PlayerRules.PlayerMovement playerMovement;
    public LayerMask whatIsGrappleable;
    public Transform gunTip, cam, player;
    private float maxDistance = 100f;
    private SpringJoint joint;
    private bool canGrapple = true;
    RaycastHit hit;

    void Update() 
    {
        if (playerMovement.isGrounded || playerMovement.isWallLeft || playerMovement.isWallRight) canGrapple = true;
        if (Input.GetMouseButtonDown(0) && canGrapple) 
        {
            StartGrapple();
        }
        else if (Input.GetMouseButtonUp(0)) 
        {
            StopGrapple();
            if (IsGrappling()) canGrapple = false;
        }
    }

    void StartGrapple() 
    {
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

            //The distance grapple will try to keep from grapple point. 
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            //Adjust these values to fit your game.
            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;
        }
    }


    /// <summary>
    /// Call whenever we want to stop a grapple
    /// </summary>
    void StopGrapple() 
    {
        Destroy(joint);
    }



    public bool IsGrappling() 
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint() 
    {
        return grapplePoint;
    }
}