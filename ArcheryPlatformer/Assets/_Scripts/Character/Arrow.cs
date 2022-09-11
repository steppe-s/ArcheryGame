using System;
using UnityEngine;

namespace _Scripts
{
    public abstract class Arrow : MonoBehaviour
    {
        protected abstract void OnImpact();
        protected abstract void OnImpactWithCharacter();
        protected abstract void OnImpactWithSurface();

        private void OnCollisionEnter2D(Collision2D col)
        {
            
        }

        public abstract class ArrowState
        {
            
        }

        public class QuiverArrowState : ArrowState
        {
            
        }
        
        public class FlyingArrowState : ArrowState
        {
            
        }
        
        public class AbilityArrowState : ArrowState
        {
            
        }
        
        public class StuckArrowState : ArrowState
        {
            
        }
    }
}
