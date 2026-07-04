using System;
using System.Collections.Generic;
using UnityEngine;

namespace ViceBayEmpire.Data
{
    /// <summary>The kinds of objective the MissionManager can drive.</summary>
    public enum ObjectiveType
    {
        GoTo,               // reach a position (on foot or in a vehicle)
        ReachOnFoot,        // reach a position, must be out of a vehicle
        EnterVehicleClass,  // get into a vehicle of a class (Car/Boat/Plane/Helicopter)
        StealVehicle,       // enter the specific vehicle spawned at the objective position
        DriveToLocation,    // reach a position while in a vehicle
        FlyThrough,         // reach a position while airborne (plane/heli)
        Kill,               // eliminate the target spawned for this objective
        Rob,                // finish robbing the location near this position
        Deliver,            // reach the drop-off (models "deliver the item")
        ReachWanted,        // raise the wanted level to N stars
        LoseWanted,         // escape the cops (wanted back to 0)
        HoldPosition,       // stay near a position for timeLimit seconds (defend)
        DialogueChoice,     // make a branching dialogue choice
        OpenDarkWeb,        // tutorial: open the Dark Web on the phone
        OpenBusiness,       // tutorial: open the Business tab on the phone
        BuyAnyProperty      // tutorial: purchase any property
    }

    /// <summary>One serializable objective step within a mission.</summary>
    [Serializable]
    public class ObjectiveSpec
    {
        public ObjectiveType type;
        [TextArea] public string text = "";        // HUD line
        public Vector3 position;
        public float radius = 6f;
        public float timeLimit = 0f;               // >0 = timed; fail if exceeded
        public int wantedStars = 0;                // for ReachWanted
        public string vehicleClass = "Car";        // Car/Boat/Plane/Helicopter
        public bool spawnTargetGang = true;        // Kill: spawn a gang target
        public float targetHealth = 80f;

        // DialogueChoice fields
        public string speaker = "";
        public string line = "";
        public string choiceA = "", choiceB = "";
        public int repA = 1, repB = 0;
    }

    /// <summary>
    /// A mission definition. Built at runtime via ScriptableObject.CreateInstance in
    /// MissionContent (so no .asset authoring is needed), but authorable as an asset too.
    /// </summary>
    [CreateAssetMenu(menuName = "ViceBay/Mission", fileName = "Mission_")]
    public class MissionData : ScriptableObject
    {
        public string id;
        public string title;
        public string giver = "Unknown";
        public bool isTutorial;
        public bool autoStartNext;                 // chain straight into the next mission

        [Tooltip("Where the mission giver beacon appears (for non-auto missions).")]
        public Vector3 giverPosition;

        [TextArea] public List<string> briefing = new();   // "Speaker|Line" barks at start
        public List<ObjectiveSpec> objectives = new();

        [Header("Rewards")]
        public long rewardMoney = 1000;
        public int rewardReputation = 1;
        public string unlockNote = "";             // shown on completion
    }
}
