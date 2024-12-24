using UnityEngine;

namespace JCGodSwornConfigurator
{
    internal class ModSpectatorMode
    {
        private bool initialized = false;
        public void InitializeSpectatorMode(DataManager dataManager)
        {
            if (initialized) return;

            foreach (var map in dataManager.availableMaps)
            {
                if (!map.IsCampaignMap && !map.IsChallangeMap)
                {
                    map.MaxParticipants++;
                    map.MaxPlayers++;
                    map.SpawnerLocations.Add(Vector2.zero);
                    map.HerospawnLocations.Add(Vector2.zero);
                }
            }

            initialized = true;
        }
    }
}
