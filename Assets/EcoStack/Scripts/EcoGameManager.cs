using UnityEngine;
using System;

public class EcoGameManager : MonoBehaviour
{
    public static EcoGameManager Instance { get; private set; }

    [Header("Economía")]
    public int ecoCoins = 0;

    [Header("Estadísticas Mejorables")]
    public int capacidadMaximaTorre = 3;
    public float velocidadJugador = 6f;
    public int escudosActuales = 0;
    public int escudosMaximos = 3;

    [Header("Costos de Tienda")]
    public int costoCapacidad = 50;
    public int costoVelocidad = 50;
    public int costoEscudo = 40;

    // Evento para actualizar automáticamente la UI (Integrante 3) y al Jugador (Integrante 1)
    public event Action OnStatsActualizadas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Llamado por los Hábitats al recolectar dinero
    public void AgregarCoins(int cantidad)
    {
        ecoCoins += cantidad;
        Debug.Log($"Eco-Coins actuales: {ecoCoins}");
        OnStatsActualizadas?.Invoke();
    }

    // --- FUNCIONES PÚBLICAS PARA LOS BOTONES DE LA TIENDA ---

    public bool ComprarCapacidad()
    {
        if (ecoCoins >= costoCapacidad)
        {
            ecoCoins -= costoCapacidad;
            capacidadMaximaTorre += 1;
            costoCapacidad = Mathf.RoundToInt(costoCapacidad * 1.5f);
            OnStatsActualizadas?.Invoke();
            return true;
        }
        return false;
    }

    public bool ComprarVelocidad()
    {
        if (ecoCoins >= costoVelocidad)
        {
            ecoCoins -= costoVelocidad;
            velocidadJugador += 1.2f;
            costoVelocidad = Mathf.RoundToInt(costoVelocidad * 1.5f);
            OnStatsActualizadas?.Invoke();
            return true;
        }
        return false;
    }

    public bool ComprarEscudo()
    {
        if (ecoCoins >= costoEscudo && escudosActuales < escudosMaximos)
        {
            ecoCoins -= costoEscudo;
            escudosActuales += 1;
            OnStatsActualizadas?.Invoke();
            return true;
        }
        return false;
    }

    // Llamado cuando un obstáculo golpea al jugador
    public bool ConsumirEscudo()
    {
        if (escudosActuales > 0)
        {
            escudosActuales--;
            OnStatsActualizadas?.Invoke();
            return true; // El escudo absorbió el golpe
        }
        return false; // No tenía escudo, pierde animales
    }
}