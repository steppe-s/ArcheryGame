using System;
using FishNet.Object;
using UnityEngine;

namespace _Scripts.Level
{
    [RequireComponent(typeof(NetworkObject))]
    public class SurfaceBehaviour : MonoBehaviour
    {
        private float myVariable = 1;
        private float b = 5;
        
        private void Start()
        {
            var a = CoolFunctionButWith(myVariable);
            Debug.Log(a);
        }


        

        float CoolFunctionButWith(float parameter)
        {
            return parameter + 1;
        }


    }
}
