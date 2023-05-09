using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class CharacterMovement : MonoBehaviour {
    public float speed = 300;
    public Animator anim;
    public CharacterController characterController;

    [SerializeField] private float slopeForce = 60;
    [SerializeField] private float slopeForceRayLength = 30;
    [SerializeField] private float gravity = -9.8f;
    private Vector3 velocity;
    [HideInInspector] public Vector3 forwardVector;


    public virtual void Move(Vector3 inputMovement) {
        inputMovement = inputMovement.normalized;
        forwardVector = GetForward(inputMovement);
        Vector3 moveDirection = inputMovement * speed;

        if (!IsGrounded()) {
            moveDirection *= 0.2f;
        }

        characterController.SimpleMove(-moveDirection * Time.fixedDeltaTime);

        if ((inputMovement.x != 0 || inputMovement.z != 0) && OnSlope()) {
            characterController.Move(Vector3.down * characterController.height / 2 * slopeForce * Time.deltaTime);
        }

        anim.SetFloat("movementX", inputMovement.x);
        anim.SetFloat("movementY", inputMovement.z);
        anim.SetFloat("Speed", inputMovement.sqrMagnitude);

        if (inputMovement.sqrMagnitude > 0.01f) {
            if (Mathf.Abs(inputMovement.x) > Mathf.Abs(inputMovement.z)) {
                anim.SetFloat("Dir", inputMovement.x > 0 ? 0.6f : 1);
            } else {
                anim.SetFloat("Dir", inputMovement.z > 0 ? 0.4f : 0);
            }
        }
    }

    public Vector3 GetForward(Vector3 inputMovement) {
        inputMovement = inputMovement.normalized;
        if (inputMovement.magnitude > 0.01f) {
            return Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up) * inputMovement;
        } else {
            return forwardVector;
        }
    }

    public virtual void ApplyGravity() {
        if (!IsGrounded()) {
            velocity.y += gravity * Time.fixedDeltaTime;
            characterController.Move(velocity * Time.fixedDeltaTime);
        } else {
            velocity.y = 0;
        }
        
    }

    private bool IsGrounded() {
        RaycastHit hit;
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, characterController.height / 2 +1.5f);
        Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.red);
        return isGrounded;
    }


    private bool OnSlope() {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, characterController.height / 2 * slopeForceRayLength)) {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle != 0 && slopeAngle <= slopeForce) {
                return true;
            }
        }
        return false;
    }
}