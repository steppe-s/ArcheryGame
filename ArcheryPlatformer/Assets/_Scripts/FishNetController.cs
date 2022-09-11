using System;
using System.Collections.Generic;
using _Scripts.Utils;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class FishNetController : NetworkBehaviour
    {
        #region Types.

        private struct InputData
        {
            public Vector2 MoveVector;
            public Vector2 AimVector;
            public bool Sprint;

            public InputData(Vector2 moveVector, Vector2 aimVector, bool sprint)
            {
                MoveVector = moveVector;
                AimVector = aimVector;
                Sprint = sprint;
            }
        }

        private struct ReconcileData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector2 Velocity;
            public float AngularVelocity;
            public float LastJump;
            
            public ReconcileData(Vector3 position, Quaternion rotation, Vector2 velocity, float angularVelocity, float lastJump)
            {
                Position = position;
                Rotation = rotation;
                Velocity = velocity;
                AngularVelocity = angularVelocity;
                LastJump = lastJump;
            }
        }

        #endregion

        #region Inspector Fields.

        [SerializeField] protected float aimSpeed;
        [SerializeField] protected float walkingSpeed;
        [SerializeField] protected float walkingForce;
        [SerializeField] protected float aerialSpeed;
        [SerializeField] protected float aerialForce;
        [SerializeField] protected float sprintingSpeedMultiplier;
        [SerializeField] protected float sprintingForceMultiplier;
        [SerializeField] protected float jumpingStrength;
        [SerializeField] protected float jumpingCooldown;
        [SerializeField] protected float horizontalVelocityCutoff;
        [SerializeField] protected float diveStrength; 
        [SerializeField] protected float diveSpeed;
        [SerializeField] protected float maxRampAngle = 60f;
        [SerializeField] protected float rampCheckDistance;
        
        [SerializeField] private GameObject bow;
        [SerializeField] private Transform rampPointLeft, rampPointRight;
        [SerializeField] private LayerMask groundLayers;
        
        [SerializeField] private List<Collider2D> grounders;
        [SerializeField] private List<Collider2D> grippers;
        
        #endregion
        
        #region ReadOnly Fields.

        [SerializeField, ReadOnly] private Vector2 movementInput;
        [SerializeField, ReadOnly] private Vector2 aimInput;
        [SerializeField, ReadOnly] private bool sprint;
        [SerializeField, ReadOnly] private bool subscribed = false;
        [SerializeField, ReadOnly] private float lastJump = 0f;
        [SerializeField, ReadOnly] private bool gripped;
        [SerializeField, ReadOnly] private bool grounded;

        #endregion

        #region Private Fields.

        private Rigidbody2D _rigidbody;
        private Camera _camera;

        private bool _groundCheckDone;
        private bool _gripCheckDone;
        
        #endregion

        #region Controls.

        private Controls _controls;
        private Controls Controls
        {
            get
            {
                if (_controls != null)
                {
                    return _controls;
                }
                return _controls = new Controls();
            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (prevOwner == Owner)
            {
                Controls.Disable();
                return;
            }
            Controls.Enable();
            _rigidbody.isKinematic = false;
        }
        
        private Vector2 CalcAimVector(InputAction.CallbackContext ctx)
        {
            // if (_input.currentControlScheme != "Keyboard&Mouse") return ctx.ReadValue<Vector2>();
            return (_camera.ScreenToWorldPoint(ctx.ReadValue<Vector2>()) - transform.position).normalized;
        }
        
        private InputData PackageInputData()
        {
            return new InputData(movementInput, aimInput, sprint);
        }

        #endregion
        
        #region Start.

        public override void OnStartClient()
        {
            base.OnStartClient();

            _camera = Camera.main;
            _rigidbody = GetComponent<Rigidbody2D>();
            
            Controls.Player.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
            Controls.Player.Move.canceled += ctx => movementInput = Vector2.zero;
            Controls.Player.Aim.performed += ctx => aimInput = CalcAimVector(ctx);
            Controls.Player.Sprint.performed += ctx => sprint = true;
            Controls.Player.Sprint.canceled += ctx => sprint = false;
            if (!IsServer)
            {
                _rigidbody.isKinematic = true;
            }
            Controls.Disable();
            SubscribeToTimeManager(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _rigidbody = GetComponent<Rigidbody2D>();
            SubscribeToTimeManager(true);
        }

        #endregion

        #region Stop.

        // public override void OnDespawnServer(NetworkConnection connection)
        // {
        //     base.OnDespawnServer(connection);
        //     Controls.Disable();
        //     SubscribeToTimeManager(false);
        // }
        //
        // public override void OnStopClient()
        // {
        //     base.OnStopClient();
        //     Controls.Disable();
        //     SubscribeToTimeManager(false);
        // }

        private void OnDestroy()
        {
            Controls.Disable();
            SubscribeToTimeManager(false);
        }

        #endregion

        #region Loop.

        private void SubscribeToTimeManager(bool subscribe)
        {
            TimeManager timeManager = InstanceFinder.TimeManager;
            if (timeManager == null)
                return;
            if (subscribe == subscribed)
                return;
            subscribed = subscribe;

            if (subscribe)
            {
                timeManager.OnTick += TimeManager_OnTick;
                timeManager.OnPostTick += TimeManager_OnPostTick;
            }
            else
            {
                timeManager.OnTick -= TimeManager_OnTick;
                timeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }
        
        private void TimeManager_OnTick()
        {
            if (IsOwner)
            {
                Reconcile(default, false);
                var data = PackageInputData();
                Act(data, false);
            }
            if (IsServer)
            {
                Act(default, true);
            }
        }
        
        private void TimeManager_OnPostTick()
        {
            if (!IsServer) return;
            var t = transform;
            var data = new ReconcileData(
                t.position, 
                t.rotation, 
                _rigidbody.velocity, 
                _rigidbody.angularVelocity,
                lastJump);
            Reconcile(data, true);
        }

        #endregion

        #region Prediction Methods.
        
        [Replicate]
        private void Act(InputData data, bool asServer, bool replaying = false)
        {
            _groundCheckDone = false;
            _gripCheckDone = false;
            Aim(data.AimVector);
            HorizontalInput(data);
            if (data.MoveVector.y > 0) Jump();
            else if (data.MoveVector.y < 0) Dive();
        }

        [Reconcile]
        private void Reconcile(ReconcileData data, bool asServer)
        {
            var t = transform;
            t.position = data.Position;
            t.rotation = data.Rotation;
            _rigidbody.velocity = data.Velocity;
            _rigidbody.angularVelocity = data.AngularVelocity;
        }
        
        #endregion

        #region Controller methods.

        private void Aim(Vector2 aimVector)
        {
            float angle = Mathf.Atan2(aimVector.normalized.y, aimVector.normalized.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            bow.transform.rotation = Quaternion.Slerp(bow.transform.rotation, q, Time.deltaTime * aimSpeed);
        }

        private void HorizontalInput(InputData data)
        {
            if (data.MoveVector.x == 0)
            {
                Brake();
            }
            else
            {
                WalkOrGlide(data.MoveVector, data.Sprint);
            }
        }
        
        private void WalkOrGlide(Vector2 moveVector, bool doSprint)
        {
            if (Grounded())
            {
                Walk(moveVector.x, doSprint);
            }
            else
            {
                Glide(moveVector.x);
            }
        }

        private void Walk(float input, bool doSprint)
        {
            var force = doSprint ? walkingForce * sprintingForceMultiplier : walkingForce;
            var speed = doSprint ? walkingSpeed * sprintingSpeedMultiplier : walkingSpeed;
            AddXAxisForce(input, force, speed, true);
        }

        private void Glide(float input)
        {
            AddXAxisForce(input, aerialForce, aerialSpeed, true);
        }
        
        private void Brake()
        {
            if (Mathf.Abs(_rigidbody.velocity.x) < horizontalVelocityCutoff && Grounded())
            {
                _rigidbody.velocity = new Vector2(0, _rigidbody.velocity.y);
            }
        }

        private void Dive()
        {
            
        }

        private void Jump()
        {
            if (!(Time.time - lastJump >= jumpingCooldown)) return;
            lastJump = Time.time;
            if (!Grounded() && !Gripped()) return;
            _rigidbody.AddForce(Vector2.up * jumpingStrength);
        }

        private void AddXAxisForce(float input, float acceleration, float max, bool doAngleCheck)
        {
            var velocity = _rigidbody.velocity;
            if (!(Mathf.Abs(velocity.x) < max) && !(velocity.x * input < 0) && velocity.x != 0) return;
            var vector = doAngleCheck ? AngleCheck(input) : Vector2.right;
            _rigidbody.AddForce(vector.normalized * (input * acceleration), 0f);
        }
        
        private Vector2 AngleCheck(float direction)
        {
            var vector = GetPlaneAlignedMovementVector2(direction);
            return Mathf.Abs(Vector2.Angle(vector, Vector2.right)) > maxRampAngle ? Vector2.zero : vector;
        }
        
        private Vector2 GetPlaneAlignedMovementVector2(float direction)
        {
            if (direction == 0f) return Vector2.right;
            var point = direction < 0 ? rampPointLeft : rampPointRight;
            var hit = Physics2D.Raycast(point.position, Vector2.down, rampCheckDistance, groundLayers);
            return hit.collider ? MathUtilities.RotateVectorByDelta(hit.normal, -90 * Mathf.Deg2Rad) : Vector2.right;
        }

        private bool Grounded()
        {
            if (_groundCheckDone) return grounded;
            return grounded = grounders.Find(t => t.IsTouchingLayers(groundLayers));
        }

        private bool Gripped()
        {
            if (_gripCheckDone) return gripped;
            return gripped = grippers.Find(t => t.IsTouchingLayers(groundLayers));
        }

        #endregion
    }
}
