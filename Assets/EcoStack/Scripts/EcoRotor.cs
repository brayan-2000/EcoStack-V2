using UnityEngine;
namespace EcoStack
{
    public class EcoRotor : MonoBehaviour
    {
        EcoStackGame game;
        void Start() { game=GetComponentInParent<EcoStackGame>(); }
        void Update() { if(game==null || !game.SimulationStopped) transform.Rotate(0,900*Time.deltaTime,0,Space.Self); }
    }
}
