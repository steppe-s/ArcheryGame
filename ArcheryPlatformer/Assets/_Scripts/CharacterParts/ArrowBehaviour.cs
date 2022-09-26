using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using Unity.VisualScripting;
using UnityEngine;

namespace _Scripts.CharacterParts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ArrowBehaviour : NetworkBehaviour
    {
        private static List<ArrowBehaviour> _all;
        public static List<ArrowBehaviour> All => _all;

        [SerializeField, ReadOnly] protected Rigidbody2D rb;
        [SerializeField, ReadOnly] protected Collider2D col;
        [SerializeField, ReadOnly] protected NetworkTransform netTransform;
        [SerializeField, ReadOnly] public List<Collider2D> ignoredColliders;
        [SerializeField, ReadOnly] protected ArrowState state;
        [SerializeField, ReadOnly] protected Transform connectedNockPosition;
        
        [SerializeField] protected Transform nockPosition;
        [SerializeField] protected Transform pointPosition;
        
        
        protected IArrowInventory OwnerInventory;
        public ArrowState State => state;

        #region Network events.

        public override void OnStartClient()
        {
            base.OnStartClient();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            netTransform = GetComponent<NetworkTransform>();
            col.isTrigger = true;
            if (!IsServer) rb.isKinematic = true;
            RegisterArrow(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            netTransform = GetComponent<NetworkTransform>();
            col.isTrigger = true;
            RegisterArrow(true);
        }

        private void RegisterArrow(bool register)
        {
            _all ??= new List<ArrowBehaviour>();
            if (register)
            {
                _all.Add(this);
            }
            else
            {
                _all.Remove(this);
            }
        }
        
        #endregion
        
        #region Unity events.

        private void Update()
        {
            switch (state)
            {
                case ArrowState.Flight:
                    OnUpdateFlight(); break;
                case ArrowState.Stuck:
                    OnUpdateStuck(); break;
                case ArrowState.Quiver:
                    OnUpdateQuiver(); break;
                case ArrowState.Nock: 
                    OnUpdateNock(); break;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            switch (state)
            {
                case ArrowState.Flight:
                    OnTriggerEnterFlight(other); break;
                case ArrowState.Stuck:
                    OnTriggerEnterStuck(other); break;
                case ArrowState.Quiver:
                    OnTriggerEnterQuiver(other); break;
                case ArrowState.Nock: 
                    OnTriggerEnterNock(other); break;
            }            
        }

        private void OnDestroy()
        {
            RegisterArrow(false);
        }

        #endregion

        #region States.

        public enum ArrowState
        {
            Flight, Stuck, Quiver, Nock
        }

        #region Flight state.

        protected void EnterFlightState()
        {
            OwnerInventory = null;
            rb.isKinematic = false;
            connectedNockPosition = null;
            state = ArrowState.Flight;
        }
        
        protected void OnUpdateFlight()
        {
            if (!IsServer) return;
            var velocity = rb.velocity;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 10);
        }

        protected void OnTriggerEnterFlight(Collider2D other)
        {
            if (ignoredColliders.Contains(other)) return;
            if (!other.GetComponent<DamageableBehaviour>() && !other.GetComponent<SurfaceBehaviour>()) return;
            // StuckInObject(other.transform);
            ExitFlightState();
            EnterStuckState();
        }

        protected void ExitFlightState()
        {
            
        }

        #endregion

        #region Stuck state.

        protected void EnterStuckState()
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            state = ArrowState.Stuck;
        }
        
        protected void OnUpdateStuck()
        {
            
        }

        protected void OnTriggerEnterStuck(Collider2D other)
        {
            
        }

        #endregion

        #region Quiver state.

        protected void OnUpdateQuiver()
        {
        }

        protected void OnTriggerEnterQuiver(Collider2D other)
        {
            
        }

        #endregion

        #region Nock state.

        protected void OnUpdateNock()
        {
            if (!connectedNockPosition) return;
            transform.position = connectedNockPosition.position;
            transform.rotation = connectedNockPosition.rotation;
        }

        protected void OnTriggerEnterNock(Collider2D other)
        {
            
        }

        #endregion

        #endregion

        public void Release(Vector3 position, Vector2 velocity, List<Collider2D> ignoreColliders)
        {
            if (state != ArrowState.Nock) return;
            transform.position = position;
            rb.velocity = velocity;
            ignoredColliders = ignoreColliders;
            EnterFlightState();
        }

        public bool Nock(IArrowInventory inventory, Transform stringPoint)
        {
            if (state is not (ArrowState.Stuck or ArrowState.Nock or ArrowState.Quiver)) return false;
            OwnerInventory = inventory;
            connectedNockPosition = stringPoint;
            state = ArrowState.Nock;
            return true;
        }

        public bool Quiver(IArrowInventory inventory)
        {
            if (state is not (ArrowState.Stuck or ArrowState.Nock or ArrowState.Quiver)) return false;
            OwnerInventory = inventory;
            state = ArrowState.Quiver;
            return true;
        }

        public bool RemoveFromInventory(IArrowInventory inventory)
        {
            if (inventory != OwnerInventory) return false;
            OwnerInventory = null;
            return true;
        }

        protected void Hit(DamageableBehaviour target)
        {
            
        }
    }
}