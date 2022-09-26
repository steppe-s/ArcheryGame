using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Objects;
using _Scripts.Utils;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.CharacterParts
{
    public class BowBehaviour : TimedNetworkBehaviour, IArrowInventory
    {
        

        [SerializeField] private BowType bowType;
        [SerializeField] private Transform stringNockPoint;
        
        [Header("info")]
        [SerializeField, ReadOnly] private bool draw;
        [SerializeField, ReadOnly] private bool cancel;
        [SerializeField, ReadOnly] private float drawProgress;
        [SerializeField, ReadOnly] private float lastCancelOrRelease;
        [SerializeField, ReadOnly] private Vector2 aimInput;
        [SerializeField, ReadOnly] private List<Collider2D> ignoredColliders;
        
        [SyncVar]
        [SerializeField, ReadOnly] private ArrowBehaviour arrow;

        private Camera _camera;
        
        public float DrawProgress
        {
            get => drawProgress;
            set => drawProgress = Mathf.Clamp01(value);
        }


        #region Types.
        private struct InputData
        {
            public bool Draw;
            public bool Cancel;
            public Vector2 AimVector;

            public InputData(bool draw, bool cancel, Vector2 aimVector)
            {
                Draw = draw;
                Cancel = cancel;
                AimVector = aimVector;
            }
        }

        private struct ReconcileData
        {
            public float DrawProgress;
            public Quaternion Rotation;

            public ReconcileData(float drawProgress, Quaternion rotation)
            {
                DrawProgress = drawProgress;
                Rotation = rotation;
            }
        }
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
        }

        private InputData PackageInputData()
        {
            return new InputData(draw, cancel, aimInput);
        }
        
        private Vector2 CalcAimVector(InputAction.CallbackContext ctx)
        {
            // if (_input.currentControlScheme != "Keyboard&Mouse") return ctx.ReadValue<Vector2>();
            return (_camera.ScreenToWorldPoint(ctx.ReadValue<Vector2>()) - transform.position).normalized;
        }
        
        #endregion
        
        #region Start.

        private void Start()
        {
            ignoredColliders = transform.parent.GetComponentsInChildren<Collider2D>().ToList();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _camera = Camera.main;
            Controls.Player.Draw.performed += ctx => draw = true;
            Controls.Player.Draw.canceled += ctx => draw = false;
            Controls.Player.Cancel.performed += ctx => cancel = true;
            Controls.Player.Cancel.canceled += ctx => cancel = false;
            Controls.Player.Aim.performed += ctx => aimInput = CalcAimVector(ctx);
            Controls.Disable();
            SubscribeToTimeManager(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SubscribeToTimeManager(true);
        }

        private void OnDestroy()
        {
            Controls.Disable();
            SubscribeToTimeManager(false);
        }
        
        #endregion

        #region Loop.

        protected override void TimeManager_OnTick()
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

        protected override void TimeManager_OnPostTick()
        {
            if (!IsServer) return;
            var data = new ReconcileData(DrawProgress, transform.rotation);
            Reconcile(data, true);
        }

        #endregion
        
        #region Prediction Methods.
        
        [Replicate]
        private void Act(InputData data, bool asServer, bool replaying = false)
        {
            Draw(data, asServer);
            Aim(data.AimVector);
        }

        [Reconcile]
        private void Reconcile(ReconcileData data, bool asServer)
        {
            DrawProgress = data.DrawProgress;
            transform.rotation = data.Rotation;
        }
        
        #endregion
        
        private void Draw(InputData data, bool asServer)
        {
            if (data.Cancel)
            {
                DrawProgress = 0;
                draw = false;
                lastCancelOrRelease = (float) TimeManager.TicksToTime(TickType.Tick);
            }
            else if (data.Draw && lastCancelOrRelease + bowType.Cooldown < (float) TimeManager.TicksToTime(TickType.Tick) && arrow)
            {
                DrawProgress += ((float) TimeManager.TickDelta) / bowType.DrawTime;
            }
            else if (DrawProgress > 0 && arrow)
            {
                if (IsServer)
                {
                    arrow.Release(transform.position,(transform.rotation * Vector2.right * (bowType.DrawCurve.Evaluate(drawProgress) * bowType.Velocity)), ignoredColliders);
                    arrow = null;
                    
                    // GameObject newArrow = Instantiate(arrowPrefab);
                    // newArrow.transform.rotation = transform.rotation;
                    // newArrow.transform.position = transform.position;
                    // newArrow.GetComponent<Rigidbody2D>().velocity = transform.rotation * Vector2.right * (drawProgress * 20);
                    // newArrow.GetComponent<ArrowBehaviour>().ignoredColliders = ignoredColliders;
                    // Spawn(newArrow);
                }
                DrawProgress = 0;
                lastCancelOrRelease = (float) TimeManager.TicksToTime(TickType.Tick);
            }
        }

        private void Aim(Vector2 aimVector)
        {
            float angle = Mathf.Atan2(aimVector.normalized.y, aimVector.normalized.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * bowType.AimSpeed);
        }

        public bool TryAddArrowToInventory(ArrowBehaviour newArrow)
        {
            if (arrow || lastCancelOrRelease + bowType.Cooldown >= (float) TimeManager.TicksToTime(TickType.Tick)) return false;
            
            arrow = newArrow;
            return arrow.Nock(this, stringNockPoint);
        }

        public ArrowBehaviour GetArrowFromInventory(int index)
        {
            return arrow;
        }

        public ArrowBehaviour RemoveArrowFromInventory(int index)
        {
            if (!arrow) return null;
            var t = arrow;
            arrow.RemoveFromInventory(this);
            arrow = null;
            return t;
        }

        public ArrowBehaviour SwapArrow(ArrowBehaviour newArrow, int index)
        {
            var t = arrow;
            arrow = newArrow;
            return t;
        }

        public List<ArrowBehaviour> GetArrowsFromInventory()
        {
            return new List<ArrowBehaviour> {arrow};
        }
    }
}
