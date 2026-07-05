using UnityEngine;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Builds a simple blocky humanoid out of primitives (head, torso, arms, legs) as
    /// child meshes under a character root, so the player and NPCs read as *people*
    /// instead of floating capsules. Purely cosmetic — the CharacterController on the
    /// root still does the collision/movement. Feet sit at the root's local origin so it
    /// lines up with a 2m-tall controller.
    /// </summary>
    public static class CharacterFactory
    {
        public static void BuildHumanoid(Transform root, Color shirt)
        {
            var skin = new Color(0.82f, 0.65f, 0.52f);
            var pants = new Color(0.18f, 0.19f, 0.24f);
            var shoes = new Color(0.08f, 0.08f, 0.09f);
            var hair = new Color(0.12f, 0.09f, 0.07f);

            var body = new GameObject("Body").transform;
            body.SetParent(root);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;

            // legs
            Part(body, PrimitiveType.Capsule, new Vector3(-0.12f, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), pants);
            Part(body, PrimitiveType.Capsule, new Vector3(0.12f, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), pants);
            // shoes
            Part(body, PrimitiveType.Cube, new Vector3(-0.12f, 0.06f, 0.05f), new Vector3(0.2f, 0.12f, 0.34f), shoes);
            Part(body, PrimitiveType.Cube, new Vector3(0.12f, 0.06f, 0.05f), new Vector3(0.2f, 0.12f, 0.34f), shoes);
            // torso
            Part(body, PrimitiveType.Capsule, new Vector3(0, 1.15f, 0), new Vector3(0.46f, 0.42f, 0.28f), shirt);
            // arms
            Part(body, PrimitiveType.Capsule, new Vector3(-0.34f, 1.15f, 0), new Vector3(0.14f, 0.42f, 0.14f), shirt);
            Part(body, PrimitiveType.Capsule, new Vector3(0.34f, 1.15f, 0), new Vector3(0.14f, 0.42f, 0.14f), shirt);
            // hands
            Part(body, PrimitiveType.Sphere, new Vector3(-0.34f, 0.78f, 0), new Vector3(0.14f, 0.14f, 0.14f), skin);
            Part(body, PrimitiveType.Sphere, new Vector3(0.34f, 0.78f, 0), new Vector3(0.14f, 0.14f, 0.14f), skin);
            // head + hair
            Part(body, PrimitiveType.Sphere, new Vector3(0, 1.72f, 0), new Vector3(0.26f, 0.28f, 0.26f), skin);
            Part(body, PrimitiveType.Sphere, new Vector3(0, 1.8f, -0.02f), new Vector3(0.28f, 0.2f, 0.28f), hair);
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col) Object.Destroy(col);            // cosmetic only
            MaterialFactory.Paint(go, color, 0.25f);
        }
    }
}
