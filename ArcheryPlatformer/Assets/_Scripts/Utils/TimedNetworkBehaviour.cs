using FishNet;
using FishNet.Managing.Timing;
using FishNet.Object;
using UnityEngine;

namespace _Scripts.Utils
{
    public abstract class TimedNetworkBehaviour : NetworkBehaviour
    {
        [SerializeField, ReadOnly] protected bool subscribed = false;

        protected void SubscribeToTimeManager(bool subscribe)
        {
            TimeManager timeManager = InstanceFinder.TimeManager;
            if (timeManager == null)
                return;
            if (subscribe == subscribed)
                return;
            subscribed = subscribe;

            if (subscribe)
            {
                timeManager.OnTick += TimeManager_OnTick;
                timeManager.OnPostTick += TimeManager_OnPostTick;
            }
            else
            {
                timeManager.OnTick -= TimeManager_OnTick;
                timeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }
        protected abstract void TimeManager_OnTick();
        protected abstract void TimeManager_OnPostTick();

    }
}
