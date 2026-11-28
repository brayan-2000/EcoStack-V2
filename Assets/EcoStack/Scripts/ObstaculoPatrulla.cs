using UnityEngine;

public class ObstaculoPatrulla : MonoBehaviour
{
    [Header("Configuración de Patrullaje")]
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 3.5f;
    public bool rotarHaciaDestino = true;

    private Transform objetivoActual;

    private void Start()
    {
        objetivoActual = puntoB != null ? puntoB : puntoA;
    }

    private void Update()
    {
        if (puntoA == null || puntoB == null) return;

        // Mover hacia el objetivo actual
        transform.position = Vector3.MoveTowards(transform.position, objetivoActual.position, velocidad * Time.deltaTime);

        // Girar suavemente hacia donde avanza
        if (rotarHaciaDestino)
        {
            Vector3 direccion = (objetivoActual.position - transform.position).normalized;
            direccion.y = 0; // Mantiene el giro horizontal
            if (direccion != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), 10f * Time.deltaTime);
            }
        }

        // Cambiar de destino al llegar
        if (Vector3.Distance(transform.position, objetivoActual.position) < 0.1f)
        {
            objetivoActual = (objetivoActual == puntoA) ? puntoB : puntoA;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Primero verifica si el jugador compró un escudo en la tienda
            if (EcoGameManager.Instance != null && EcoGameManager.Instance.ConsumirEscudo())
            {
                Debug.Log("¡Escudo bloqueó el golpe del obstáculo!");
                return;
            }

            Debug.Log("¡Jugador golpeado! Derribando torre de animales.");
            // Aquí se conecta con el método de dispersión del Integrante 1 (SendMessage evita errores si aún no lo une)
            other.SendMessage("RecibirGolpeObstaculo", SendMessageOptions.DontRequireReceiver);
        }
    }
}