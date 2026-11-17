using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuController : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Panel visual del menú de pausa")]
    [SerializeField] private GameObject pausePanel;

    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena del menú principal")]
    [SerializeField] private string mainMenuSceneName = "Menu_Principal";

    private bool isPaused = false;

    /// <summary>
    /// Retorna si el juego se encuentra pausado actualmente.
    /// </summary>
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Auto-detección de pausePanel si no está asignado
        if (pausePanel == null)
        {
            Transform found = transform.Find("Panel_MenuPausa") ?? transform.Find("Panel_Pausa");
            if (found == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Panel_MenuPausa", System.StringComparison.OrdinalIgnoreCase) ||
                        t.name.Equals("Panel_Pausa", System.StringComparison.OrdinalIgnoreCase))
                    {
                        pausePanel = t.gameObject;
                        break;
                    }
                }
            }
            else
            {
                pausePanel = found.gameObject;
            }
        }

        // Búsqueda y vinculación automática de botones del menú de pausa
        BindButton(new string[] { "Boton_Reanudar", "Btn_Resume", "Reanudar" }, ResumeGame);
        BindButton(new string[] { "Boton_Reiniciar", "Boton_ReiniciarNivel", "Btn_Restart", "Reiniciar Nivel" }, RestartLevel);
        BindButton(new string[] { "Boton_MenuPrincipal", "Btn_MainMenu", "Menú Principal", "Menu Principal" }, ReturnToMainMenu);
    }

    private void Start()
    {
        // Asegurar que el juego comience sin pausa y con escala de tiempo normal
        ResumeGame();
    }

    private void Update()
    {
        bool escapeTriggered = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escapeTriggered = true;
        }
#endif

#if !ENABLE_INPUT_SYSTEM
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapeTriggered = true;
        }
#endif

        if (escapeTriggered)
        {
            TogglePause();
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
    /// Alterna el estado de pausa del juego.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pausa el juego y activa el panel de pausa.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Debug.Log("[PauseMenuController] Juego en pausa.");
    }

    /// <summary>
    /// Reanuda el juego desactivando el panel y restaurando Time.timeScale a 1f.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Debug.Log("[PauseMenuController] Juego reanudado.");
    }

    /// <summary>
    /// Restablece Time.timeScale a 1f y recarga la escena actual.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[PauseMenuController] Reiniciando nivel: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Restablece Time.timeScale a 1f y carga la escena del menú principal ("Menu_Principal").
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log($"[PauseMenuController] Regresando al menú principal: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
