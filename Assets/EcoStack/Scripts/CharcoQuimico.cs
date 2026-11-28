using UnityEngine;

public class CharcoQuimico : MonoBehaviour
{
    [Header("Ralentización")]
    [Range(0.1f, 0.9f)]
    public float multiplicadorVelocidad = 0.4f; // Reduce velocidad al 40%

    private float velocidadOriginalGuardada;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && EcoGameManager.Instance != null)
        {
            velocidadOriginalGuardada = EcoGameManager.Instance.velocidadJugador;
            EcoGameManager.Instance.velocidadJugador *= multiplicadorVelocidad;
            Debug.Log("Entró al charco químico: velocidad reducida.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && EcoGameManager.Instance != null)
        {
            EcoGameManager.Instance.velocidadJugador = velocidadOriginalGuardada;
            Debug.Log("Salió del charco químico: velocidad restaurada.");
        }
    }
}