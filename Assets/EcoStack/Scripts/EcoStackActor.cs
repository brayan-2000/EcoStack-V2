using UnityEngine;
namespace EcoStack
{
    [DisallowMultipleComponent]
    public class EcoStackActor : MonoBehaviour
    {
        public enum Role { Animal, Robot, Drone, Turret, Boss, Chemical, Conveyor, Medkit }
        public Role role;
        [Range(0,3), Tooltip("0 Ajolote, 1 Tortuga, 2 Gorila, 3 Pangolín. Cambia la lógica, no sustituye el modelo.")]
        public int species;
        [Min(1)] public int maximumHealth=95;
        [Min(.1f), Tooltip("Radio aproximado del cuerpo para recibir disparos.")]
        public float hitRadius=.85f;
        [Tooltip("Indicador de aviso; se oculta hasta que el enemigo prepara un disparo.")]
        public Transform warningLight;
    }
}
