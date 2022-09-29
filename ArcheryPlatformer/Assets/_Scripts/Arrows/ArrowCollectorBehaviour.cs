using System.Collections.Generic;
using System.Linq;
using _Scripts.CharacterParts;
using FishNet.Object;
using UnityEngine;

namespace _Scripts.Arrows
{
    public class ArrowCollectorBehaviour : NetworkBehaviour
    {
        private List<IArrowInventory> _inventories;

        [SerializeField] private float range;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _inventories = GetComponents<IArrowInventory>().ToList();
            _inventories.AddRange(GetComponentsInChildren<IArrowInventory>().ToList());
        }

        private void LateUpdate()
        {
            if (!IsServer) return;
            ArrowBehaviour.All.FindAll(
                arrow => 
                    arrow.CanBePickedUp &&
                    arrow.State == ArrowBehaviour.ArrowState.Stuck &&
                    Vector2.Distance(arrow.transform.position, transform.position) <= range &&
                    transform.parent != transform
                    ).ForEach(arrow =>
            {
                _inventories.Find(inventory => inventory.TryAddArrowToInventory(arrow));
            });
        }
    }

    public interface IArrowInventory
    {
        public bool TryAddArrowToInventory(ArrowBehaviour newArrow);
        public ArrowBehaviour GetArrowFromInventory(int index);
        public ArrowBehaviour RemoveArrowFromInventory(int index);
        public ArrowBehaviour SwapArrow(ArrowBehaviour newArrow, int index);
        public List<ArrowBehaviour> GetArrowsFromInventory();
        public Transform GetArrowTransform(int index);
        public Transform GetNextArrowTransform();
    }
}
