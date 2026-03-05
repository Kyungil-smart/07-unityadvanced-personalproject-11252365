using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState
{
    Title,
    Playing,
    Paused,
    Clear,
    GameOver
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance  { get; private set; }
    
    [Header("빌드 세팅")]
    [SerializeField] private int _titleSceneBuildIndex = 0;
    [SerializeField] private int _stage01SceneBuildIndex = 1;
    [SerializeField] private int _stage02SceneBuildIndex = 2;
    
    [Header("디버그")]
    [SerializeField] private bool _enableDebugLog = false;
    
    public GameState CurrentState { get; private set; } =  GameState.Title;
    
    public event Action<GameState> StateChanged;
    
    private RobotController _robotController;
    private RobotCombatController _robotCombatController;

    private int _currentStageBuildIndex = 1;

    #region Unity Lifecycle
    private void Awake() => Init();

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void Update()
    {
        // Playing/pause 토글 (ESC, P)
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            if (Keyboard.current != null)
            {
                bool pausePressed = 
                    Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.pKey.wasPressedThisFrame;
                
                if (pausePressed) TogglePause();
            }
        }

        if (CurrentState == GameState.Playing && _robotCombatController != null)
        {
            if (_robotCombatController.IsDead) GameOver();
        }
    }

    #endregion
    
    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    #region 씬 관리 및 흐름 제어
    public void LoadTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_titleSceneBuildIndex);
    }

    public void LoadStage01()
    {
        _currentStageBuildIndex = _stage01SceneBuildIndex;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_stage01SceneBuildIndex);
    }

    public void LoadStage02()
    {
        _currentStageBuildIndex = _stage02SceneBuildIndex;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_stage02SceneBuildIndex);
    }
    
    public void RestartCurrentStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_currentStageBuildIndex);
    }
    
    #endregion
    
    #region 게임 상태 및 코어 루프 제어
    public void Clear()
    {
        if (CurrentState == GameState.Clear || CurrentState == GameState.GameOver) return;
        
        ChangeState(GameState.Clear);
        FreezeGameplay();
        ShowCursor();

        Log("Clear()");
    }

    public void GameOver()
    {
        if (CurrentState == GameState.Clear || CurrentState == GameState.GameOver) return;
        
        ChangeState(GameState.GameOver);
        FreezeGameplay();
        ShowCursor();
        
        Log("GameOver()");
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            FreezeGameplay();
            ShowCursor();
            
            Log("Pause()");
        }
        else if (CurrentState == GameState.Paused)
        {
            Resume();
        }
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused) return;
        
        ChangeState(GameState.Playing);
        UnfreezeGameplay();
        HideCursor();
        
        Log("Resume()");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    #endregion
    
    #region 씬 로드 처리 및 참조 갱신
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int buildIndex = scene.buildIndex;

        _robotController = FindFirstObjectByType<RobotController>();
        _robotCombatController = FindFirstObjectByType<RobotCombatController>();

        if (buildIndex == _titleSceneBuildIndex)
        {
            ChangeState(GameState.Title);
            Time.timeScale = 1f;
            ShowCursor();
        }
        else
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            HideCursor();

            UnfreezeGameplay();
        }
        
        Log($"SceneLoaded: {scene.name} (Index {buildIndex})");
    }
    
    private new static T FindFirstObjectByType<T>() where T : UnityEngine.Object
    {
        return  UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }
    
    #endregion
    
    #region Gameplay Freeze / Unfreeze
    private void FreezeGameplay()
    {
        if (_robotController != null) _robotController.enabled = false;
        if (_robotCombatController != null) _robotCombatController.enabled = false;
        
        Time.timeScale = 0f;
    }

    private void UnfreezeGameplay()
    {
        Time.timeScale = 1f;
        
        if (_robotController != null) _robotController.enabled = true;
        if (_robotCombatController != null) _robotCombatController.enabled = true;
    }

    #endregion
    
    #region State / Cursor / Debug
    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        StateChanged?.Invoke(newState);
    }

    private static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Log(string message)
    {
        if (!_enableDebugLog) return;
        Debug.Log($"[GameManager]: {message}");
    }
    #endregion
    
}
