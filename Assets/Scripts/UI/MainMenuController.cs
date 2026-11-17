using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles UI")]
    [Tooltip("Panel de opciones/configuraciones")]
    [SerializeField] private GameObject optionsPanel;

    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena principal a cargar al presionar Jugar")]
    [SerializeField] private string mainLevelSceneName = "Nivel_Principal";

    private void Awake()
    {
        // Auto-detección de optionsPanel si no fue asignado en el Inspector
        if (optionsPanel == null)
        {
            Transform found = transform.Find("Panel_Opciones");
            if (found == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Equals("Panel_Opciones", System.StringComparison.OrdinalIgnoreCase))
                    {
                        optionsPanel = child.gameObject;
                        break;
                    }
                }
            }
            else
            {
                optionsPanel = found.gameObject;
            }
        }

        // Búsqueda y vinculación automática de botones por nombre para ejecución inmediata
        BindButton(new string[] { "Boton_Jugar", "Btn_Jugar", "Jugar" }, PlayGame);
        BindButton(new string[] { "Boton_Opciones", "Btn_Opciones", "Opciones" }, OpenOptions);
        BindButton(new string[] { "Boton_Salir", "Btn_Salir", "Salir" }, QuitGame);
        BindButton(new string[] { "Boton_Cerrar", "Btn_Cerrar", "Cerrar" }, CloseOptions);
    }

    private void Start()
    {
        // Asegurar que el panel de opciones comience cerrado si está asignado
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void BindButton(string[] candidateNames, UnityEngine.Events.UnityAction action)
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            foreach (string candidate in candidateNames)
            {
                if (btn.name.Equals(candidate, System.StringComparison.OrdinalIgnoreCase))
                {
                    btn.onClick.RemoveListener(action);
                    btn.onClick.AddListener(action);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Inicia el juego cargando la escena "Nivel_Principal".
    /// </summary>
    public void PlayGame()
    {
        Debug.Log($"[MainMenuController] Iniciando juego. Cargando escena: {mainLevelSceneName}");
        SceneManager.LoadScene(mainLevelSceneName);
    }

    /// <summary>
    /// Abre el panel de opciones.
    /// </summary>
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            Debug.Log("[MainMenuController] Panel de opciones abierto.");
        }
        else
        {
            Debug.LogWarning("[MainMenuController] optionsPanel no está asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Cierra el panel de opciones.
    /// </summary>
    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
            Debug.Log("[MainMenuController] Panel de opciones cerrado.");
        }
        else
        {
            Debug.LogWarning("[MainMenuController] optionsPanel no está asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Cierra la aplicación o detiene el modo Play en el editor.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuController] Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
