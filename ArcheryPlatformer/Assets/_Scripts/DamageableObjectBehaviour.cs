using UnityEngine;
using UnityEngine.Events;

namespace _Scripts
{
    public class DamageableObjectBehaviour : MonoBehaviour
    {
        [SerializeField, ReadOnly] private float health;
        public float Health
        {
            get => health;
            private set => health = Mathf.Clamp(value, 0, maxHealth);
        }


        [SerializeField] private float maxHealth;
        [SerializeField] private UnityEvent onDeath;

        public void Damage(float amount)
        {
            Health -= amount;
            if (Health > 0) return;
            Health = 0;
            onDeath.Invoke();
        }

        public void Heal(float amount)
        {
            Health += amount;
        }
    }
}
