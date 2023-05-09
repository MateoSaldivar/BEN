using UnityEngine;

public class PlayerMovement : CharacterMovement {
    void Update() {
    
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputMovement = new Vector3(horizontal, 0, vertical);
        Move(inputMovement);
        ApplyGravity();
    }
}