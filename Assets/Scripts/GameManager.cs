using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Screens")]
    [SerializeField] private GameObject titleScreenObject;
    [SerializeField] private GameObject winScreenObject;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Transitions")]
    [SerializeField] private float cameraAnimDuration  = 1.8f;
    [SerializeField] private float fadeDuration        = 1.5f;
    [SerializeField] private Image blackPanel;
    [SerializeField] private Image whitePanel;
    [SerializeField] private CameraMovement player;
    [SerializeField] private float winThreshold=100f;
    [SerializeField] private float loseThreshold=120f;
    
    private bool waitingForInput = false;
    private bool isDead = false;

    private static bool hasLaunched = false; // survives scene reloads

    void Awake()
    {
        Instance = this;
        titleScreenObject?.SetActive(false);
        winScreenObject?.SetActive(false);
        if (blackPanel != null) blackPanel.color = Color.clear;
        if (whitePanel != null) whitePanel.color = Color.clear;
    }

    void Start()
    {
        if (!hasLaunched)
            StartCoroutine(ShowTitleScreenNextFrame());
        else
        {
            promptText?.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private IEnumerator ShowTitleScreenNextFrame()
    {
        yield return null;
        ShowTitleScreen();
    }

    void Update()
    {
        if (waitingForInput && Input.anyKeyDown)
            StartRun();

        if (!isDead && !waitingForInput)
            CheckDeathCondition();
    }
    

    private void ShowTitleScreen()
    {
        waitingForInput = true;
        titleScreenObject?.SetActive(true);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Press anything to open your eyes";
            Canvas promptCanvas = promptText.GetComponentInParent<Canvas>();
            if (promptCanvas != null)
            {
                promptCanvas.enabled = false;
                promptCanvas.enabled = true;
            }
        }
    }

    private void ShowDeathScreen()
    {
        // title screen stays hidden — black screen is already the background
        titleScreenObject?.SetActive(false);
        if (promptText != null) promptText.text = "Press anything to open your eyes";
        promptText?.gameObject.SetActive(true);
        waitingForInput = true;
    }

    private void StartRun()
    {
        waitingForInput = false;
        isDead          = false;
        titleScreenObject?.SetActive(false);
        promptText?.gameObject.SetActive(false);
        Time.timeScale  = 1f;

        if (hasLaunched)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        hasLaunched = true;
    }
    

    private void CheckDeathCondition()
    {
        Emotion e = Emotion.Instance;

        bool win  = e.content >= winThreshold && e.unease >= winThreshold && e.awe >= winThreshold;
        bool lose = e.content >= loseThreshold || e.unease >= loseThreshold || e.awe >= loseThreshold;

        if (win)        StartCoroutine(DoWin());
        else if (lose)  StartCoroutine(DoDeath());
    }

    private IEnumerator DoWin()
    {
        isDead = true; // reuse flag to stop further checks
        player.LookUp();

        yield return new WaitForSeconds(cameraAnimDuration);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            if (whitePanel != null) whitePanel.color = Color.Lerp(Color.clear, Color.white, t);
            yield return null;
        }

        winScreenObject?.SetActive(true);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Press anything to open your eyes";
        }
        waitingForInput = true;
    }

    private IEnumerator DoDeath()
    {
        isDead = true;
        player.FallToGround();

        yield return new WaitForSeconds(cameraAnimDuration);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            if (blackPanel != null) blackPanel.color = Color.Lerp(Color.clear, Color.black, t);
            yield return null;
        }

        ShowDeathScreen();
    }
}
