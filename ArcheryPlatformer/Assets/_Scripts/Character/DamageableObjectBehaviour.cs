using FishNet.Component.Prediction;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.Character
{
    public class DamageableObjectBehaviour : NetworkBehaviour
    {
        [SerializeField, ReadOnly, SyncVar] private float health;

        private Rigidbody2D _rigidbody2D;
        public Rigidbody2D Rigidbody2D => _rigidbody2D;


        public float Health
        {
            get => health;
            private set => health = Mathf.Clamp(value, 0, maxHealth);
        }

        [SerializeField] private Transform graphicalTransform;
        public Transform GraphicalTransform => graphicalTransform;

        [SerializeField] private float maxHealth;
        [SerializeField] private UnityEvent onDeath;
        [SerializeField] private UnityEvent onDamage;
        [SerializeField] private UnityEvent onHeal;
        [SerializeField] private UnityEvent onHealthChange;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            health = maxHealth;
        }

        public void Damage(float amount)
        {
            if (amount == 0) return;
            Health -= amount;
            onDamage.Invoke();
            onHealthChange.Invoke();
            if (Health > 0) return;
            Health = 0;
            onDeath.Invoke();
        }

        public void Heal(float amount)
        {
            if (amount == 0) return;
            Health += amount;
            onHeal.Invoke();
            onHealthChange.Invoke();
        }
    }
}
