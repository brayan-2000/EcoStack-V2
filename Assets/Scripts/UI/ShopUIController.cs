using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    [Header("Panel Principal de Tienda")]
    [Tooltip("Panel contenedor de la interfaz de la tienda")]
    [SerializeField] private GameObject shopPanel;

    [Header("Mejora 1: Capacidad de Carga")]
    [Tooltip("Texto para mostrar el nivel actual de Capacidad de Carga")]
    [SerializeField] private TMP_Text capacityLevelText;
    [Tooltip("Texto para mostrar el costo de la siguiente mejora de Capacidad")]
    [SerializeField] private TMP_Text capacityCostText;

    [Header("Mejora 2: Velocidad de Movimiento")]
    [Tooltip("Texto para mostrar el nivel actual de Velocidad de Movimiento")]
    [SerializeField] private TMP_Text speedLevelText;
    [Tooltip("Texto para mostrar el costo de la siguiente mejora de Velocidad")]
    [SerializeField] private TMP_Text speedCostText;

    [Header("Mejora 3: Escudo Protector")]
    [Tooltip("Texto para mostrar el nivel actual del Escudo Protector")]
    [SerializeField] private TMP_Text shieldLevelText;
    [Tooltip("Texto para mostrar el costo de la siguiente mejora de Escudo")]
    [SerializeField] private TMP_Text shieldCostText;

    // Eventos públicos para que el Integrante 2 (o cualquier lógica externa) pueda suscribirse
    public event Action OnBuyCapacityUpgrade;
    public event Action OnBuySpeedUpgrade;
    public event Action OnBuyShieldUpgrade;

    private void Awake()
    {
        // Auto-detección del panel de tienda si no está asignado
        if (shopPanel == null)
        {
            Transform found = transform.Find("Panel_TiendaMejoras") ?? transform.Find("Panel_Tienda");
            if (found == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Panel_TiendaMejoras", StringComparison.OrdinalIgnoreCase) ||
                        t.name.Equals("Panel_Tienda", StringComparison.OrdinalIgnoreCase))
                    {
                        shopPanel = t.gameObject;
                        break;
                    }
                }
            }
            else
            {
                shopPanel = found.gameObject;
            }
        }

        // Auto-detección de referencias de textos
        AutoAssignTexts();

        // Auto-vinculación de botones de compra y cierre
        BindButton(new string[] { "Boton_MejoraCapacidad", "Btn_Capacidad", "Boton_Capacidad" }, BuyCapacityUpgrade);
        BindButton(new string[] { "Boton_MejoraVelocidad", "Btn_Velocidad", "Boton_Velocidad" }, BuySpeedUpgrade);
        BindButton(new string[] { "Boton_MejoraEscudo", "Btn_Escudo", "Boton_Escudo" }, BuyShieldUpgrade);
        BindButton(new string[] { "Boton_CerrarTienda", "Btn_CloseShop", "Boton_Cerrar", "Cerrar" }, () => ToggleShop(false));
    }

    private void AutoAssignTexts()
    {
        if (capacityLevelText == null) capacityLevelText = FindChildComponent<TMP_Text>("Texto_NivelCapacidad") ?? FindChildComponent<TMP_Text>("Capacity_LevelText");
        if (capacityCostText == null) capacityCostText = FindChildComponent<TMP_Text>("Texto_CostoCapacidad") ?? FindChildComponent<TMP_Text>("Capacity_CostText");

        if (speedLevelText == null) speedLevelText = FindChildComponent<TMP_Text>("Texto_NivelVelocidad") ?? FindChildComponent<TMP_Text>("Speed_LevelText");
        if (speedCostText == null) speedCostText = FindChildComponent<TMP_Text>("Texto_CostoVelocidad") ?? FindChildComponent<TMP_Text>("Speed_CostText");

        if (shieldLevelText == null) shieldLevelText = FindChildComponent<TMP_Text>("Texto_NivelEscudo") ?? FindChildComponent<TMP_Text>("Shield_LevelText");
        if (shieldCostText == null) shieldCostText = FindChildComponent<TMP_Text>("Texto_CostoEscudo") ?? FindChildComponent<TMP_Text>("Shield_CostText");
    }

    private T FindChildComponent<T>(string targetName) where T : Component
    {
        foreach (T comp in GetComponentsInChildren<T>(true))
        {
            if (comp.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return comp;
            }
        }
        return null;
    }

    private void BindButton(string[] candidateNames, UnityEngine.Events.UnityAction action)
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            foreach (string candidate in candidateNames)
            {
                if (btn.name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    btn.onClick.RemoveListener(action);
                    btn.onClick.AddListener(action);
                    break;
                }
            }
        }
    }

    private void Start()
    {
        // Iniciar con la tienda cerrada si el panel está asignado
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Abre o cierra el panel de la tienda según el valor booleano recibido.
    /// </summary>
    /// <param name="state">True para abrir, False para cerrar.</param>
    public void ToggleShop(bool state)
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(state);
            Debug.Log($"[ShopUIController] Panel de tienda {(state ? "abierto" : "cerrado")}.");
        }
        else
        {
            Debug.LogWarning("[ShopUIController] shopPanel no está asignado.");
        }
    }

    /// <summary>
    /// Alterna el estado actual de visibilidad del panel de la tienda.
    /// </summary>
    public void ToggleShop()
    {
        if (shopPanel != null)
        {
            ToggleShop(!shopPanel.activeSelf);
        }
    }

    /// <summary>
    /// Procesa la compra de la mejora de Capacidad de Carga.
    /// </summary>
    public void BuyCapacityUpgrade()
    {
        Debug.Log("[ShopUIController] Solicitud de compra: Mejora de Capacidad de Carga.");
        // TODO: Conectar con el script Tycoon del Integrante 2
        OnBuyCapacityUpgrade?.Invoke();
    }

    /// <summary>
    /// Procesa la compra de la mejora de Velocidad de Movimiento.
    /// </summary>
    public void BuySpeedUpgrade()
    {
        Debug.Log("[ShopUIController] Solicitud de compra: Mejora de Velocidad de Movimiento.");
        // TODO: Conectar con el script Tycoon del Integrante 2
        OnBuySpeedUpgrade?.Invoke();
    }

    /// <summary>
    /// Procesa la compra de la mejora de Escudo Protector.
    /// </summary>
    public void BuyShieldUpgrade()
    {
        Debug.Log("[ShopUIController] Solicitud de compra: Mejora de Escudo Protector.");
        // TODO: Conectar con el script Tycoon del Integrante 2
        OnBuyShieldUpgrade?.Invoke();
    }

    /// <summary>
    /// Actualiza los textos de nivel y costo para la Capacidad de Carga.
    /// </summary>
    public void UpdateCapacityUI(int level, int cost)
    {
        if (capacityLevelText != null) capacityLevelText.text = $"Nivel: {level}";
        if (capacityCostText != null) capacityCostText.text = $"Costo: {cost}";
    }

    /// <summary>
    /// Actualiza los textos de nivel y costo para la Velocidad de Movimiento.
    /// </summary>
    public void UpdateSpeedUI(int level, int cost)
    {
        if (speedLevelText != null) speedLevelText.text = $"Nivel: {level}";
        if (speedCostText != null) speedCostText.text = $"Costo: {cost}";
    }

    /// <summary>
    /// Actualiza los textos de nivel y costo para el Escudo Protector.
    /// </summary>
    public void UpdateShieldUI(int level, int cost)
    {
        if (shieldLevelText != null) shieldLevelText.text = $"Nivel: {level}";
        if (shieldCostText != null) shieldCostText.text = $"Costo: {cost}";
    }
}
