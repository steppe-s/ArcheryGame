using System.Collections;
using System.Collections.Generic;
using _Scripts.Character;
using _Scripts.CharacterParts;
using UnityEngine;

public class PoisonArrow : ArrowBehaviour
{
    [SerializeField] private float damage;
    protected override void OnHitDamageableObjectServerOnly(GameObject damageable, Vector2 relativeVelocity)
    {
        base.OnHitDamageableObjectServerOnly(damageable, relativeVelocity);
        var d = damageable.GetComponent<DamageableObjectBehaviour>();
        d.Damage(damage);
    }
}
