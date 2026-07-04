using UnityEngine;
using ViceBayEmpire.Play;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Builds drivable vehicles out of primitives at runtime (no models needed) and
    /// configures a DriveableVehicle for the right class. Good enough to drive, crash,
    /// and explode immediately; swap the primitive body for an Asset Store model later
    /// by parenting it under the returned root and deleting the placeholder mesh.
    /// </summary>
    public static class VehicleFactory
    {
        public static DriveableVehicle Build(DriveableVehicle.Mode mode, Vector3 pos, Color color, string name)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            GameObject body;
            switch (mode)
            {
                case DriveableVehicle.Mode.Boat:
                    body = Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(2.4f, 0.8f, 5f), color);
                    Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.7f, -0.5f), new Vector3(1.6f, 0.9f, 2f), color * 0.7f);
                    break;
                case DriveableVehicle.Mode.Plane:
                    body = Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(1f, 1f, 5.5f), color);
                    Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0, 0.4f), new Vector3(8f, 0.2f, 1.2f), color * 0.85f); // wings
                    Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.6f, -2.4f), new Vector3(0.2f, 1.2f, 1f), color * 0.7f); // tail
                    break;
                case DriveableVehicle.Mode.Helicopter:
                    body = Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 1.4f, 3f), color);
                    Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.2f, -3f), new Vector3(0.3f, 0.3f, 3f), color * 0.7f); // tail boom
                    var rotor = Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(7f, 0.1f, 0.4f), Color.black);
                    rotor.AddComponent<Spinner>();
                    break;
                default: // Car
                    body = Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(2f, 0.7f, 4.2f), color);
                    Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.6f, -0.2f), new Vector3(1.8f, 0.7f, 2.2f), color * 0.8f); // cabin
                    break;
            }

            // seat + exit anchors
            var seat = new GameObject("Seat").transform; seat.SetParent(root.transform); seat.localPosition = new Vector3(0, 0.9f, 0);
            var exit = new GameObject("Exit").transform; exit.SetParent(root.transform); exit.localPosition = new Vector3(2.2f, 0.5f, 0);

            var dv = root.AddComponent<DriveableVehicle>();
            dv.mode = mode;
            dv.displayName = name;
            dv.seat = seat; dv.exitPoint = exit;
            switch (mode)
            {
                case DriveableVehicle.Mode.Car:
                    dv.topSpeed = 32; dv.accel = 18; dv.turn = 100; dv.grip = 7; dv.maxHealth = 400; break;
                case DriveableVehicle.Mode.Boat:
                    dv.topSpeed = 26; dv.accel = 12; dv.turn = 55; dv.maxHealth = 500; break;
                case DriveableVehicle.Mode.Plane:
                    dv.topSpeed = 60; dv.accel = 16; dv.turn = 45; dv.liftSpeed = 18; dv.stallSpeed = 11;
                    dv.verticalRate = 6; dv.maxHealth = 250; break;
                case DriveableVehicle.Mode.Helicopter:
                    dv.topSpeed = 34; dv.accel = 14; dv.turn = 70; dv.verticalRate = 9; dv.maxHealth = 260; break;
            }
            return dv;
        }

        static GameObject Prim(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            MaterialFactory.Paint(go, color);
            return go;
        }
    }

    /// <summary>Spins a helicopter rotor for visual life.</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 1400f;
        void Update() => transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
    }
}
