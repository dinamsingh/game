using System.Collections.Generic;
using UnityEngine;

namespace RoxxParking
{
    public sealed class CourseMeshLifetime : MonoBehaviour
    {
        private readonly HashSet<Mesh> owned = new HashSet<Mesh>();
        public void Own(Mesh mesh) { if (mesh != null) owned.Add(mesh); }
        private void OnDestroy() { foreach (Mesh mesh in owned) Destroy(mesh); }
    }
}
