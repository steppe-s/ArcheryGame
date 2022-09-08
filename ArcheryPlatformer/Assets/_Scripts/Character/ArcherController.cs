using System;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Character
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ArcherController : MonoBehaviour
    {
        [Header("Movement")] 
        [SerializeField] protected float walkingSpeed;
        [SerializeField] protected float walkingForce, aerialSpeed, aerialForce, jumpingStrength, jumpingCooldown, horizontalVelocityCutoff = 0.5f, diveStrength, diveSpeed;

        [Header("Aim")] 
        [SerializeField] protected float armRotateSpeed;
        
        [Header("Grounding")]
        [SerializeField] private Transform pointA, pointB;
        [SerializeField] private LayerMask groundLayers;
        
        [Header("info")]
        [ReadOnly, SerializeField] protected float lastJump;
        [ReadOnly, SerializeField] protected Vector2 movementVector, lookVector;
        [ReadOnly, SerializeField] private bool grounded, groundCheckHasBeenDone;
        [ReadOnly, SerializeField] private Bow bow;
        
        private Rigidbody2D _rb;
        private Collider2D _col;
        private Camera _camera;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _camera = Camera.main;
            bow = GetComponentInChildren<Bow>();
            lastJump = Time.time;
        }

        private void Update()
        {
            Vector2 vectorToTarget = _camera.ScreenToWorldPoint(lookVector) - transform.position;
            float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            bow.transform.rotation = Quaternion.Slerp(bow.transform.rotation, q, Time.deltaTime * armRotateSpeed);
        }

        private void FixedUpdate()
        {
            MovementFixedUpdate();
        }

        private void MovementFixedUpdate()
        {
            if (movementVector.x != 0)
            {
                if (IsGrounded())
                {
                    HorizontalMovement(walkingForce, walkingSpeed, true);
                }
                else
                {
                    HorizontalMovement(aerialForce, aerialSpeed, false);
                }
            }
            else
            {
                if (_rb.velocity.x != 0 && Mathf.Abs(_rb.velocity.x) < horizontalVelocityCutoff && IsGrounded())
                {
                    _rb.velocity = new Vector2(0, _rb.velocity.y);
                }
            }

            switch (movementVector.y)
            {
                case > 0:
                    Jump();
                    break;
                case < 0 when !IsGrounded():
                    Dive();
                    break;
            }
        }

        private void HorizontalMovement(float acceleration, float max, bool doAngleCheck)
        {
            var velocity = _rb.velocity;

            if (Mathf.Abs(velocity.x) < max || velocity.x * movementVector.x < 0 || velocity.x == 0)
            {
                var forceToApply = movementVector.x * acceleration;
                var vector = Vector2.right;
                if (doAngleCheck)
                {
                    vector = GetPlaneAlignedMovementVector2(vector);
                }
                _rb.AddForce(vector.normalized * forceToApply, 0f);
            }
        }

        private Vector2 GetPlaneAlignedMovementVector2(Vector2 original)
        {
            var y = Mathf.Max(pointA.position.y, pointB.position.y);
            var x = movementVector.x > 0 ? Mathf.Max(pointA.position.x, pointB.position.x) : Mathf.Min(pointA.position.x, pointB.position.x);
            var distance = y - Mathf.Min(pointA.position.y, pointB.position.y);
            var hit = Physics2D.Raycast(new Vector2(x, y), Vector2.down, distance, groundLayers);
            return hit.collider ? Rotate(hit.normal, -90 * Mathf.Deg2Rad) : original;
        }

        private Vector2 Rotate(Vector2 v, float delta) {
            return new Vector2(
                v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
                v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
            );
        }
        
        private void Jump()
        {
            if (!(Time.time - lastJump >= jumpingCooldown)) return;
            lastJump = Time.time;
            if (!IsGrounded()) return;
            _rb.AddForce(Vector2.up * jumpingStrength);
        }

        private void Dive()
        {
            if (_rb.velocity.y > 0)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, 0);
            }
            if (Mathf.Abs(_rb.velocity.y) < diveSpeed)
            {
                _rb.AddForce(Vector2.down * diveStrength);
            }
        }

        private bool IsGrounded()
        {
            if (groundCheckHasBeenDone) return grounded;
            if (!pointA || !pointB) return false;
            grounded = Physics2D.OverlapArea(pointA.position, pointB.position, groundLayers);
            return grounded;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!pointA || !pointB) return;
            var positionA = pointA.position;
            var positionB = pointB.position;
            Gizmos.DrawLine(positionA, new Vector3(positionA.x, positionB.y));
            Gizmos.DrawLine(positionA, new Vector3(positionB.x, positionA.y));
            Gizmos.DrawLine(positionB, new Vector3(positionA.x, positionB.y));
            Gizmos.DrawLine(positionB, new Vector3(positionB.x, positionA.y));
        }
    }
    
}
