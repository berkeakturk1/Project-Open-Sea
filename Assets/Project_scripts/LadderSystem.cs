using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

[RequireComponent(typeof(BoxCollider))]
public class LadderSystem : MonoBehaviour
{
    [Header("Ladder Settings")]
    public float climbSpeed        = 0.0000000000001f;    // Units per second you climb
    public float horizontalOffset  = 0.1f;  // How tightly you hug the ladder
    public float ladderTopOffset   = 1.0f;  // How far above the top trigger you get released
    public float exitImpulse       = 3f;    // Impulse strength when you pop off

    private BoxCollider ladderCol;
    private RigidbodyFirstPersonController fpController;
    private Rigidbody            rb;
    private Transform            t;
    private bool                 onLadder;
    private float                topY;

    void Start()
    {
        ladderCol = GetComponent<BoxCollider>();
        if (!ladderCol)
        {
            Debug.LogError("SimpleLadderSystem needs a BoxCollider!");
            enabled = false;
            return;
        }
        // calculate top-of-ladder world Y
        topY = transform.position.y + (ladderCol.size.y * 0.5f * transform.localScale.y);
    }

    void OnTriggerEnter(Collider other)
    {
        var fp = other.GetComponent<RigidbodyFirstPersonController>();
        if (fp)
        {
            fpController = fp;
            rb           = fp.GetComponent<Rigidbody>();
            t            = fp.transform;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (rb == null || t == null) return;
        if (other.GetComponent<RigidbodyFirstPersonController>() == null) return;

        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(v) > 0.1f)
        {
            if (!onLadder) EnterLadderMode();

            // climb up/down
            Vector3 climbDelta = Vector3.up * climbSpeed * v * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + climbDelta);

            // hug ladder: push you back toward ladder plane
            Vector3 ladderPlane = transform.position;
            ladderPlane.y = rb.position.y;
            Vector3 toward = (ladderPlane - rb.position).normalized * horizontalOffset * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + toward);

            // if you climb above the top + offset, pop off
            if (rb.position.y > topY + ladderTopOffset)
                ExitLadder(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RigidbodyFirstPersonController>() != null)
            ExitLadder(false);
    }

    private void EnterLadderMode()
    {
        onLadder         = true;
        rb.useGravity    = false;
        rb.velocity      = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints   = RigidbodyConstraints.FreezeRotation;
        fpController.enabled = false;
    }

    private void ExitLadder(bool poppedOffTop)
    {
        if (!onLadder) return;
        onLadder           = false;
        rb.useGravity      = true;
        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints     = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
        fpController.enabled = true;

        if (poppedOffTop)
        {
            // Teleport you slightly above and in front of the ladder top
            Vector3 horizForward = -transform.forward;
            horizForward.y = 0;
            horizForward.Normalize();

            Vector3 exitPos = new Vector3(
                rb.position.x,
                topY + 0.1f,                     // just above ladder top
                rb.position.z
            ) + horizForward * 1.5f;            // 1.5m clear of the ladder

            // Instantly set your position
            rb.position = exitPos;
            // If you prefer, you can also do: transform.position = exitPos;
        }
    }


    // all physics moves happen in FixedUpdate
    void FixedUpdate() { /* nothing here—OnTriggerStay uses MovePosition */ }
}
