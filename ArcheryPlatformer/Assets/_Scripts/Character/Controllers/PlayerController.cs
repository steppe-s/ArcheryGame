using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Character.Controllers
{
    public class PlayerController : ArcherController
    {
        private void OnMove(InputValue value)
        {
            movementVector = value.Get<Vector2>();
        }
        
        void OnLook(InputValue value)
        {
            lookVector = value.Get<Vector2>();
        }
    }
}
