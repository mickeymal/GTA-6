using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.DarkWeb
{
    /// <summary>
    /// Anonymous hitman / bounty contracts sourced from the Dark Web. Accepting a
    /// contract spawns/marks a target; eliminating it pays crypto. Failing (target
    /// escapes or you die) burns the contract and dings reputation.
    /// </summary>
    public class ContractBoard : MonoBehaviour
    {
        public static ContractBoard Instance { get; private set; }

        [Serializable]
        public class Contract
        {
            public string id;
            public string targetName;
            public Vector3 lastKnownLocation;
            public double rewardVC;
            public bool active;
            public bool completed;
        }

        public List<Contract> contracts = new();
        public GameObject targetMarkerPrefab;

        EconomyManager Economy => GameManager.Instance.economy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void AcceptContract(string id)
        {
            var c = contracts.Find(x => x.id == id);
            if (c == null || c.completed) return;
            c.active = true;
            GameEvents.RaiseNotify($"Contract accepted: eliminate {c.targetName}.", NotifyType.Warning);
            if (targetMarkerPrefab != null)
                Instantiate(targetMarkerPrefab, c.lastKnownLocation, Quaternion.identity);
        }

        /// <summary>Called by the target's health component when it dies.</summary>
        public void ReportTargetKilled(string contractId, GameObject killer)
        {
            var c = contracts.Find(x => x.id == contractId && x.active && !x.completed);
            if (c == null) return;
            c.completed = true; c.active = false;
            Economy.AddCrypto(c.rewardVC);
            GameEvents.RaiseNotify($"Contract fulfilled: +{c.rewardVC:F2} VC", NotifyType.Success);
            if (DarkWebMarketplace.Instance != null) DarkWebMarketplace.Instance.reputation += 2;
        }
    }
}
