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
    public abstract class ArrowBehaviour : NetworkBehaviour
    {
        private static List<ArrowBehaviour> _all;
        public static List<ArrowBehaviour> All => _all;
        
        [SerializeField] protected Transform nockPosition;
        [SerializeField] protected Transform pointPosition;
        
        [Header("info")]
        [SerializeField, ReadOnly] protected Rigidbody2D rb;
        [SerializeField, ReadOnly] protected Collider2D col;
        [SerializeField, ReadOnly] public List<Collider2D> ignoredColliders;
        [SerializeField, ReadOnly] protected ArrowState state;
        
        protected IArrowInventory OwnerInventory;
        public ArrowState State => state;

        #region Network events.

        public override void OnStartClient()
        {
            base.OnStartClient();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (!IsServer) rb.isKinematic = true;
            RegisterArrow(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
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
            if (IsServer)
            {
                switch (state)
                {
                    case ArrowState.Flight:
                        OnFlightUpdateServerOnly(); break;
                    case ArrowState.Stuck:
                        OnStuckUpdateServerOnly(); break;
                    case ArrowState.Quiver:
                        break;
                    case ArrowState.Nock: 
                        OnNockUpdateServerOnly(); break;
                }
            }
            switch (state)
            {
                case ArrowState.Flight:
                    OnFlightUpdate(); break;
                case ArrowState.Stuck:
                    OnStuckUpdate(); break;
                case ArrowState.Quiver:
                    break;
                case ArrowState.Nock: 
                    OnNockUpdate(); break;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            if (ignoredColliders.Contains(other)) return;

            if (state == ArrowState.Flight)
            {
                PossibleHit(other);
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

        protected void SwitchStates(ArrowState next)
        {
            switch (state)
            {
                case ArrowState.Flight:
                    OnFlightExitServerOnly();
                    OnFlightExit();
                    break;
                case ArrowState.Nock:
                    OnNockExitServerOnly();
                    OnNockExit();
                    break;
                case ArrowState.Stuck:
                    OnStuckExitServerOnly();
                    OnStuckExit();
                    break;
                case ArrowState.Quiver:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            state = next;
            switch (state)
            {
                case ArrowState.Flight:
                    OnFlightEnterServerOnly();
                    OnFlightEnter();
                    break;
                case ArrowState.Nock:
                    OnNockEnterServerOnly();
                    OnNockEnter();
                    break;
                case ArrowState.Stuck:
                    OnStuckEnterServerOnly();
                    OnStuckEnter();
                    break;
                case ArrowState.Quiver:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region hooks

        protected virtual void OnNockEnter()
        {
        }
        protected virtual void OnNockEnterServerOnly()
        {
        }
        protected virtual void OnNockUpdate()
        {
        }

        protected virtual void OnNockUpdateServerOnly()
        {
        }

        protected virtual void OnNockExit()
        {
        }

        protected virtual void OnNockExitServerOnly()
        {
        }

        protected virtual void OnFlightEnter()
        {
            OwnerInventory = null;
            rb.isKinematic = false;
        }

        protected virtual void OnFlightEnterServerOnly()
        {
            
        }

        protected  void OnFlightUpdate()
        {
        }

        protected  void OnFlightUpdateServerOnly()
        {
            var velocity = rb.velocity;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 10);
        }

        protected virtual void OnFlightExit()
        {
        }

        protected virtual void OnFlightExitServerOnly()
        {
        }

        protected virtual void OnStuckEnter()
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            state = ArrowState.Stuck;
        }

        protected virtual void OnStuckEnterServerOnly()
        {
        }

        protected virtual void OnStuckUpdate()
        {
        }

        protected virtual void OnStuckUpdateServerOnly()
        {
        }

        protected virtual void OnStuckExit()
        {
        }

        protected virtual void OnStuckExitServerOnly()
        {
        }

        protected virtual void OnDrawProgress(float progress)
        {
        }

        protected virtual void OnDrawProgressServerOnly(float progress)
        {
        }

        protected virtual void OnHitSurface(SurfaceBehaviour surface)
        {
        }

        protected virtual void OnHitSurfaceServerOnly(SurfaceBehaviour surface)
        {
        }

        protected virtual void OnHitDamageableObject(DamageableObjectBehaviour damageable)
        {
        }

        protected virtual void OnHitDamageableObjectServerOnly(DamageableObjectBehaviour damageable)
        {
        }

        protected virtual void OnRelease()
        {
        }

        protected virtual void OnReleaseServerOnly()
        {
        }

        protected void OnPickup()
        {
        }

        protected void OnPickupServerOnly()
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
            SwitchStates(ArrowState.Flight);
        }

        public void PossibleHit(Collider2D other)
        {
            
            var surface = other.GetComponent<SurfaceBehaviour>();
            var damageable = other.GetComponent<DamageableObjectBehaviour>();
            if (damageable || surface)
            {
                SwitchStates(ArrowState.Stuck);
            }
            if (surface)
            {
                if (IsServer) OnHitSurfaceServerOnly(surface);
                OnHitSurface(surface);
            }
            if (damageable)
            {
                if (IsServer) OnHitDamageableObjectServerOnly(damageable);
                OnHitDamageableObject(damageable);
            }
            
        }
        
        public bool Nock(IArrowInventory inventory)
        {
            if (state is not (ArrowState.Stuck or ArrowState.Nock or ArrowState.Quiver)) return false;
            OwnerInventory = inventory;
            SwitchStates(ArrowState.Nock);
            return true;
        }

        public bool Quiver(IArrowInventory inventory)
        {
            if (state is not (ArrowState.Stuck or ArrowState.Nock or ArrowState.Quiver)) return false;
            OwnerInventory = inventory;
            SwitchStates(ArrowState.Quiver);
            return true;
        }

        public bool RemoveFromInventory(IArrowInventory inventory)
        {
            if (inventory != OwnerInventory) return false;
            OwnerInventory = null;
            return true;
        }
    }
}