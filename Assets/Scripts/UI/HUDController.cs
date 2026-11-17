using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HUDController : MonoBehaviour
{
    // Singleton sencillo para acceso global desde los scripts de los otros integrantes
    public static HUDController Instance { get; private set; }

    [Header("Referencias UI - Textos")]
    [Tooltip("Texto para mostrar las monedas ecológicas recolectadas")]
    [SerializeField] private TMP_Text ecoCoinsText;

    [Tooltip("Texto para mostrar la capacidad del stack de animales")]
    [SerializeField] private TMP_Text stackCapacityText;

    [Header("Referencias UI - Escudo")]
    [Tooltip("Barra de relleno del escudo protector")]
    [SerializeField] private Image shieldFillBar;

    [Tooltip("Ícono o contenedor visual del escudo protector")]
    [SerializeField] private GameObject shieldIcon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[HUDController] Ya existe una instancia de HUDController. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Auto-asignación defensiva de referencias si no fueron asignadas en el Inspector
        AutoAssignReferences();
        BindHeaderButtons();
    }

    private void AutoAssignReferences()
    {
        if (ecoCoinsText == null)
        {
            ecoCoinsText = FindChildComponent<TMP_Text>("EcoCoins_Text") ?? FindChildComponent<TMP_Text>("Texto_EcoCoins");
        }

        if (stackCapacityText == null)
        {
            stackCapacityText = FindChildComponent<TMP_Text>("StackCapacity_Text") ?? FindChildComponent<TMP_Text>("Texto_Torre");
        }

        if (shieldFillBar == null)
        {
            shieldFillBar = FindChildComponent<Image>("Shield_FillBar") ?? FindChildComponent<Image>("Shield_Fill");
        }

        if (shieldIcon == null)
        {
            Transform found = transform.Find("Shield_Indicator");
            if (found == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Shield_Indicator", System.StringComparison.OrdinalIgnoreCase) ||
                        t.name.Equals("Shield_Icon", System.StringComparison.OrdinalIgnoreCase))
                    {
                        shieldIcon = t.gameObject;
                        break;
                    }
                }
            }
            else
            {
                shieldIcon = found.gameObject;
            }
        }
    }

    private void BindHeaderButtons()
    {
        // Vincular botón de Pausa superior
        Button pauseBtn = FindChildComponent<Button>("Boton_Pausa") ?? FindChildComponent<Button>("PauseButton");
        if (pauseBtn != null)
        {
            PauseMenuController pauseCtrl = GetComponent<PauseMenuController>() ?? GetComponentInParent<PauseMenuController>();
            if (pauseCtrl != null)
            {
                pauseBtn.onClick.RemoveListener(pauseCtrl.TogglePause);
                pauseBtn.onClick.AddListener(pauseCtrl.TogglePause);
            }
        }

        // Vincular botón de Tienda superior
        Button shopBtn = FindChildComponent<Button>("Boton_Tienda") ?? FindChildComponent<Button>("ShopButton");
        if (shopBtn != null)
        {
            ShopUIController shopCtrl = GetComponent<ShopUIController>() ?? GetComponentInParent<ShopUIController>();
            if (shopCtrl != null)
            {
                shopBtn.onClick.RemoveListener(shopCtrl.ToggleShop);
                shopBtn.onClick.AddListener(shopCtrl.ToggleShop);
            }
        }
    }

    private T FindChildComponent<T>(string targetName) where T : Component
    {
        foreach (T comp in GetComponentsInChildren<T>(true))
        {
            if (comp.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return comp;
            }
        }
        return null;
    }

    /// <summary>
    /// Actualiza el contador de Eco-Coins en pantalla con el formato 'Eco-Coins: X'.
    /// </summary>
    /// <param name="amount">Cantidad actual de Eco-Coins.</param>
    public void UpdateEcoCoins(int amount)
    {
        if (ecoCoinsText != null)
        {
            ecoCoinsText.text = $"Eco-Coins: {amount}";
        }
        else
        {
            Debug.LogWarning("[HUDController] ecoCoinsText no está asignado.");
        }
    }

    /// <summary>
    /// Actualiza la capacidad del stack de animales con el formato 'Animales: current / max'.
    /// </summary>
    /// <param name="current">Cantidad actual de animales apilados.</param>
    /// <param name="max">Capacidad máxima permitida de la pila.</param>
    public void UpdateStackCount(int current, int max)
    {
        if (stackCapacityText != null)
        {
            stackCapacityText.text = $"Torre: {current} / {max}";
        }
        else
        {
            Debug.LogWarning("[HUDController] stackCapacityText no está asignado.");
        }
    }

    /// <summary>
    /// Activa o desactiva el indicador visual del escudo protector y ajusta el porcentaje de llenado.
    /// </summary>
    /// <param name="isActive">Estado activo/inactivo del escudo.</param>
    /// <param name="fillAmount">Porcentaje de durabilidad/carga del escudo (0.0 a 1.0).</param>
    public void SetShieldState(bool isActive, float fillAmount = 1f)
    {
        if (shieldIcon != null)
        {
            shieldIcon.SetActive(isActive);
        }

        if (shieldFillBar != null)
        {
            shieldFillBar.gameObject.SetActive(isActive);
            shieldFillBar.fillAmount = Mathf.Clamp01(fillAmount);
        }

        if (shieldIcon == null && shieldFillBar == null)
        {
            Debug.LogWarning("[HUDController] Ningún indicador de escudo está asignado.");
        }
    }
}
