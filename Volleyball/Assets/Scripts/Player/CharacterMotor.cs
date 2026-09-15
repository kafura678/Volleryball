using UnityEngine;
namespace Volleyball
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        public CourtDefinition Court;
        public TeamId Team;
        public PrototypeSettings Settings;
        public Vector3 MoveDirection { get; set; }
        public bool CanMove { get; set; } = true;
        public float JumpOffset { get; private set; }
        public bool IsJumping => jumpTime >= 0;
        CharacterController body;
        float jumpTime = -1;
        void Awake() { body = GetComponent<CharacterController>(); }
        public bool BeginJump()
        {
            if (IsJumping) return false;
            jumpTime = 0; return true;
        }
        void FixedUpdate()
        {
            if (!Court || !Settings) return;
            float dt = Time.fixedDeltaTime;
            if (jumpTime >= 0)
            {
                jumpTime += dt;
                float t = Mathf.Clamp01(jumpTime / Settings.jumpDuration);
                JumpOffset = 4 * Settings.jumpHeight * t * (1-t);
                if (t >= 1) { jumpTime = -1; JumpOffset = 0; }
            }
            Vector3 direction = Vector3.ClampMagnitude(new Vector3(MoveDirection.x,0,MoveDirection.z),1);
            Vector3 target = transform.position + (CanMove ? direction * Settings.moveSpeed * dt : Vector3.zero);
            target = Court.Clamp(target,Team);
            target.y = JumpOffset;
            body.Move(target-transform.position);
            if (direction.sqrMagnitude > 0.01f && CanMove)
                transform.rotation = Quaternion.LookRotation(direction);
        }
        public void ResetPosition(Vector3 position)
        {
            body = GetComponent<CharacterController>();
            body.enabled = false; transform.position = position; body.enabled = true;
            MoveDirection = Vector3.zero; jumpTime = -1; JumpOffset = 0;
            transform.rotation = Quaternion.LookRotation(Vector3.forward * CourtDefinition.Direction(Team));
        }
        public void ApplyNetworkPose(Vector3 position, Quaternion rotation)
        {
            body = body ? body : GetComponent<CharacterController>();
            body.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            body.enabled = true;
        }
        public void Face(Vector3 target)
        {
            Vector3 d=target-transform.position; d.y=0;
            if(d.sqrMagnitude>0.01f) transform.rotation=Quaternion.LookRotation(d);
        }
    }
}
