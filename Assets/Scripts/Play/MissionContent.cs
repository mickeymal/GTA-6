using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Builds the full mission chain at runtime (no .asset authoring needed): a guided
    /// two-part tutorial that teaches movement, driving, boats, shooting, robbing, the
    /// Dark Web and businesses — then a 10-mission story arc from petty crime to kingpin.
    /// Coordinates line up with the CityBuilder map (downtown, slums, harbor, airport,
    /// bank, store, properties).
    /// </summary>
    public static class MissionContent
    {
        public static List<MissionData> Build()
        {
            var list = new List<MissionData>();

            // ============================ TUTORIAL ============================
            list.Add(M("tut1", "First Steps", "Rico", isTut: true, autoNext: true,
                money: 1500, rep: 1,
                briefing: new[] { "Rico|Welcome to Vice Bay, kid. Let's see if you can walk straight." },
                objs: new[]
                {
                    O(ObjectiveType.ReachOnFoot, "Walk to the glowing marker (WASD, hold Shift to sprint)", new Vector3(14, 1, -22)),
                    O(ObjectiveType.EnterVehicleClass, "Steal a car — walk up to one and press F", new Vector3(6, 1, -30), vclass: "Car"),
                    O(ObjectiveType.DriveToLocation, "Drive to the marker (W/S throttle, A/D steer)", new Vector3(58, 1, -6), radius: 10),
                    O(ObjectiveType.Kill, "Aim with RMB, fire with LMB — drop the target", new Vector3(40, 1, -16)),
                }));

            list.Add(M("tut2", "Hustle 101", "Rico", isTut: true, autoNext: false,
                money: 2500, rep: 1,
                briefing: new[] { "Rico|Money's made on the water and online. Let me show you the ropes." },
                objs: new[]
                {
                    O(ObjectiveType.ReachOnFoot, "Head to the harbor pier", new Vector3(-58, 1, 80), radius: 8),
                    O(ObjectiveType.EnterVehicleClass, "Board the speedboat (press F)", new Vector3(-58, 1, 86), vclass: "Boat"),
                    O(ObjectiveType.DriveToLocation, "Drive the boat out to the buoy", new Vector3(0, 1, 100), radius: 12),
                    O(ObjectiveType.Rob, "Rob Rob's Liquor — aim at the teller, press E, then E to demand", new Vector3(-88, 1, 9), radius: 14),
                    O(ObjectiveType.OpenDarkWeb, "Open your phone (P) and browse the Dark Web tab", Vector3.zero),
                    O(ObjectiveType.OpenBusiness, "On the phone, open the Business tab", Vector3.zero),
                }, unlock: "Tutorial complete — the city is open. Follow the purple markers for story jobs."));

            // ============================ STORY ============================
            list.Add(M("s1", "Small Time", "Rico", giver: new Vector3(4, 0, -28),
                money: 3000, rep: 1,
                briefing: new[] { "Rico|Everyone starts at the bottom. Rough up a mark and grab his ride." },
                objs: new[]
                {
                    O(ObjectiveType.Kill, "Take out the target downtown", new Vector3(30, 1, 20)),
                    O(ObjectiveType.StealVehicle, "Grab the getaway car", new Vector3(36, 1, 26), vclass: "Car"),
                    O(ObjectiveType.DriveToLocation, "Get to the safehouse in the slums", new Vector3(-72, 1, -46), radius: 10),
                }));

            list.Add(M("s2", "Corner Store", "Rico", giver: new Vector3(-70, 0, -44),
                money: 4000, rep: 1,
                briefing: new[] { "Rico|Cash flow problem. There's a liquor store in the slums. You know what to do." },
                objs: new[]
                {
                    O(ObjectiveType.Rob, "Rob Rob's Liquor", new Vector3(-88, 1, 9), radius: 14),
                    O(ObjectiveType.LoseWanted, "Lose the cops", Vector3.zero),
                }));

            list.Add(M("s3", "Air Time", "Marisol", giver: new Vector3(-86, 0, -46),
                money: 6000, rep: 1,
                briefing: new[] { "Marisol|Time you learned to fly. Get to the airport and take the stunt plane up." },
                objs: new[]
                {
                    O(ObjectiveType.ReachOnFoot, "Get to the plane on the runway", new Vector3(-80, 1, -90), radius: 10),
                    O(ObjectiveType.EnterVehicleClass, "Board the plane — build speed on the runway, then hold SPACE to climb", new Vector3(-80, 1, -92), vclass: "Plane"),
                    O(ObjectiveType.FlyThrough, "Fly through the ring", new Vector3(-40, 22, -70), radius: 14),
                    O(ObjectiveType.FlyThrough, "Through the next ring", new Vector3(15, 24, -55), radius: 14),
                }));

            list.Add(M("s4", "Wet Work", "Marisol", giver: new Vector3(10, 0, -30),
                money: 8000, rep: 1,
                briefing: new[] { "Marisol|A package is floating in the bay. Take a boat, grab it, drop it on the coast." },
                objs: new[]
                {
                    O(ObjectiveType.ReachOnFoot, "Get to the harbor", new Vector3(-58, 1, 80), radius: 8),
                    O(ObjectiveType.EnterVehicleClass, "Take a boat", new Vector3(-58, 1, 86), vclass: "Boat"),
                    O(ObjectiveType.DriveToLocation, "Recover the floating package", new Vector3(35, 1, 100), radius: 12, time: 90),
                    O(ObjectiveType.Deliver, "Deliver it to the beach drop", new Vector3(88, 1, -8), radius: 12, time: 110),
                }));

            list.Add(M("s5", "Hit List", "Marisol", giver: new Vector3(10, 0, 8),
                money: 10000, rep: 2,
                briefing: new[] { "Marisol|Two Cartel names need erasing. Make it clean." },
                objs: new[]
                {
                    O(ObjectiveType.Kill, "Eliminate the rival in the slums", new Vector3(-95, 1, -28)),
                    O(ObjectiveType.Kill, "Take out his lieutenant", new Vector3(-100, 1, 12)),
                }));

            list.Add(M("s6", "The Big Score", "Marisol", giver: new Vector3(0, 0, -28),
                money: 25000, rep: 2,
                briefing: new[] { "Marisol|Vice National. Skeleton security tonight. Hit the vault and don't get boxed in." },
                objs: new[]
                {
                    O(ObjectiveType.GoTo, "Get to Vice National Bank", new Vector3(-6, 1, -10), radius: 8),
                    O(ObjectiveType.Rob, "Rob the bank — reach the vault", new Vector3(-6, 1, -14), radius: 16),
                    O(ObjectiveType.LoseWanted, "Escape the heat", Vector3.zero),
                }, unlock: "Big leagues now — build a business empire from your phone."));

            list.Add(M("s7", "Turf War", "Marisol", giver: new Vector3(-72, 0, -44),
                money: 15000, rep: 2,
                briefing: new[] { "Marisol|The slums are Cartel turf. Take the block and it pays tribute daily." },
                objs: new[]
                {
                    O(ObjectiveType.GoTo, "Move into Cartel turf", new Vector3(-95, 1, -20), radius: 12),
                    O(ObjectiveType.Kill, "Clear the corner (1)", new Vector3(-98, 1, -18)),
                    O(ObjectiveType.Kill, "Clear the corner (2)", new Vector3(-92, 1, -23)),
                    O(ObjectiveType.HoldPosition, "Hold the block", new Vector3(-95, 1, -20), radius: 26, time: 12),
                }, unlock: "Territory captured — check the gang-war zones on the map."));

            list.Add(M("s8", "Sky High", "Rico", giver: new Vector3(10, 0, -80),
                money: 18000, rep: 2,
                briefing: new[] { "Rico|Recon run. Take the chopper, hit the waypoints, bring it home." },
                objs: new[]
                {
                    O(ObjectiveType.EnterVehicleClass, "Get in the helicopter (F, SPACE to lift)", new Vector3(10, 1, -85), vclass: "Helicopter"),
                    O(ObjectiveType.FlyThrough, "Fly the recon route", new Vector3(0, 30, -40), radius: 16),
                    O(ObjectiveType.FlyThrough, "Next waypoint", new Vector3(0, 30, 20), radius: 16),
                    O(ObjectiveType.DriveToLocation, "Return and set down at the pad", new Vector3(10, 1, -85), radius: 14),
                }));

            list.Add(M("s9", "Loyalties", "Marisol", giver: new Vector3(10, 0, 8),
                money: 20000, rep: 2,
                briefing: new[] { "Marisol|Rico's been seen with the feds. Meet me at the harbor. Decide where you stand." },
                objs: new[]
                {
                    O(ObjectiveType.GoTo, "Meet Marisol at the harbor", new Vector3(-20, 1, 74), radius: 8),
                    Choice("Marisol", "Rico sold us out to the feds. What's the play?",
                           "Handle Rico — I'm loyal", 2, "Hear him out first", 0),
                    O(ObjectiveType.Kill, "Handle the situation", new Vector3(-20, 1, 70)),
                }));

            list.Add(M("s10", "Kingpin", "???", giver: new Vector3(74, 0, -34),
                money: 100000, rep: 5,
                briefing: new[] { "???|This is it. The mansion on the shore. Take it and Vice Bay is yours." },
                objs: new[]
                {
                    O(ObjectiveType.GoTo, "Assault the beach mansion", new Vector3(74, 1, -30), radius: 12),
                    O(ObjectiveType.Kill, "Cut down a guard (1)", new Vector3(70, 1, -32)),
                    O(ObjectiveType.Kill, "Cut down a guard (2)", new Vector3(78, 1, -32)),
                    O(ObjectiveType.Kill, "Take down the kingpin", new Vector3(74, 1, -40), hp: 300),
                    O(ObjectiveType.LoseWanted, "Get out alive", Vector3.zero),
                }, unlock: "VICE BAY IS YOURS. Story complete — the whole sandbox is yours to run."));

            return list;
        }

        // ---- builders --------------------------------------------------------
        static MissionData M(string id, string title, string giverName, long money, int rep,
            string[] briefing, ObjectiveSpec[] objs, Vector3 giver = default, bool isTut = false,
            bool autoNext = false, string unlock = "")
        {
            var m = ScriptableObject.CreateInstance<MissionData>();
            m.id = id; m.title = title; m.giver = giverName; m.isTutorial = isTut;
            m.autoStartNext = autoNext; m.giverPosition = giver;
            m.rewardMoney = money; m.rewardReputation = rep; m.unlockNote = unlock;
            m.briefing = new List<string>(briefing);
            m.objectives = new List<ObjectiveSpec>(objs);
            return m;
        }

        static ObjectiveSpec O(ObjectiveType type, string text, Vector3 pos, float radius = 6f,
            string vclass = "Car", float time = 0f, float hp = 80f)
            => new ObjectiveSpec { type = type, text = text, position = pos, radius = radius,
                                   vehicleClass = vclass, timeLimit = time, targetHealth = hp };

        static ObjectiveSpec Choice(string speaker, string line, string a, int repA, string b, int repB)
            => new ObjectiveSpec { type = ObjectiveType.DialogueChoice, speaker = speaker, line = line,
                                   choiceA = a, repA = repA, choiceB = b, repB = repB, text = "Make your choice" };
    }
}
