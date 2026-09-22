using UnityEngine;

namespace Aetherfall.World
{
    /// <summary>Spins, bobs and optionally orbits an object. Used to bring the placeholder level to life.</summary>
    public class FloatMotion : MonoBehaviour
    {
        public Vector3 spin = new(0f, 25f, 0f);
        public float bobHeight = 0.2f;
        public float bobSpeed = 1f;
        public Transform orbitCenter;
        public float orbitSpeed = 20f;

        Vector3 origin;
        float phase;

        void Start()
        {
            origin = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            transform.Rotate(spin * Time.deltaTime, Space.Self);
            if (orbitCenter)
            {
                transform.RotateAround(orbitCenter.position, Vector3.up, orbitSpeed * Time.deltaTime);
                return;
            }
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
        }
    }
}
