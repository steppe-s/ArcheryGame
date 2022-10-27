using _Scripts.Character;
using _Scripts.CharacterParts;
using UnityEngine;

namespace _Scripts.Arrows.Types
{
    public class NormalArrow : ArrowBehaviour
    {
        [SerializeField] private float damage;
        protected override void OnHitDamageableObjectServerOnly(GameObject damageable, Vector2 relativeVelocity)
        {
            base.OnHitDamageableObjectServerOnly(damageable, relativeVelocity);
            var d = damageable.GetComponent<DamageableObjectBehaviour>();
            d.Damage(damage);
            d.Rigidbody2D.velocity += Vector2.ClampMagnitude(relativeVelocity * rb.mass / d.Rigidbody2D.mass, relativeVelocity.magnitude);
        }
    }
}
