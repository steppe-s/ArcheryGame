using _Scripts.Character;
using _Scripts.CharacterParts;
using UnityEngine;

namespace _Scripts.Arrows.Types
{
    public class NormalArrow : ArrowBehaviour
    {
        [SerializeField] private float damage;
        protected override void OnHitDamageableObjectServerOnly(GameObject damageable)
        {
            base.OnHitDamageableObjectServerOnly(damageable);
            var d = damageable.GetComponent<DamageableObjectBehaviour>();
            d.Damage(damage);
        }
    }
}
