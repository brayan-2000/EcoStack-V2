using UnityEngine;

public class HabitatTycoon : MonoBehaviour
{
    [Header("Configuración del Hábitat")]
    public string tipoAnimal = "Ajolote";
    public int animalesAsignados = 0;
    public int capacidadHabitat = 5;

    [Header("Producción Pasiva (Tycoon)")]
    public int coinsPorMinutoPorAnimal = 60; // 60 por min = 1 moneda por segundo por animal
    public int coinsAcumulados = 0;

    private float temporizadorProduccion = 0f;

    private void Update()
    {
        if (animalesAsignados <= 0) return;

        // Calcula cuántas monedas produce por segundo según los animales dentro
        float coinsPorSegundo = (coinsPorMinutoPorAnimal / 60f) * animalesAsignados;
        temporizadorProduccion += coinsPorSegundo * Time.deltaTime;

        if (temporizadorProduccion >= 1f)
        {
            int generados = Mathf.FloorToInt(temporizadorProduccion);
            coinsAcumulados += generados;
            temporizadorProduccion -= generados;
        }
    }

    // Método público para cuando se descarguen animales rescatados hacia este recinto
    public bool AsignarAnimal()
    {
        if (animalesAsignados < capacidadHabitat)
        {
            animalesAsignados++;
            Debug.Log($"Animal añadido a {tipoAnimal}. Total en hábitat: {animalesAsignados}");
            return true;
        }
        return false;
    }

    // Recolección automática cuando el jugador camina sobre el recinto
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && coinsAcumulados > 0)
        {
            if (EcoGameManager.Instance != null)
            {
                EcoGameManager.Instance.AgregarCoins(coinsAcumulados);
            }
            Debug.Log($"Recolectados {coinsAcumulados} Eco-Coins del hábitat {tipoAnimal}.");
            coinsAcumulados = 0;
        }
    }
}
