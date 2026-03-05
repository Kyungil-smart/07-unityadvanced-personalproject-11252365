using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")] 
    [SerializeField] private GameObject _hubPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _controlsPanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("텍스트 옵션")] 
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _resultText;
    
    [Header("결과 창 메세지")]
    [SerializeField] private string _missionClearMessage = "MISSION CLEAR";
    [SerializeField] private string _gameOverMessage = "GAME OVER";

    private GameObject[] _allPanels;
    
    private RobotCombatController _robotCombatController;
    private int _lastHp = int.MinValue;
    private bool _isControlsOpen;

    #region Unity Lifecycle

    private void Awake() => Init();
    
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += HandleStateChanged;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
        
        if (_robotCombatController == null)
            _robotCombatController = FindFirstObjectByType<RobotCombatController>();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleStateChanged;
        }
    }

    private void Update() => UpdateHpDisplay();

    #endregion

    private void Init()
    {
        _allPanels = new[] { _hubPanel, _pausePanel, _controlsPanel, _resultPanel };
    }
    
    private void UpdateHpDisplay()
    {
        if (_robotCombatController == null || _hpText == null) return;
        
        int currentHp = _robotCombatController.CurrentHealth;
        if (_lastHp == currentHp) return;
        
        _lastHp = currentHp;
        _hpText.SetText($"HP : {currentHp}");
    }
    
    private void HandleStateChanged(GameState state)
    {
        HideAllPanels();
        
        switch (state)
        {
            case GameState.Playing:
                _isControlsOpen = false;
                _hubPanel?.SetActive(true);
                break;
            case GameState.Paused:
                _pausePanel?.SetActive(true);
                if (_isControlsOpen) _controlsPanel?.SetActive(true);
                break;
            case GameState.Clear:
                _isControlsOpen = false;
                _resultPanel?.SetActive(true);
                SetResultUI(_missionClearMessage);
                break;
            case GameState.GameOver:
                _isControlsOpen = false;
                _resultPanel?.SetActive(true);
                SetResultUI(_gameOverMessage);
                break;
            
            default:
                _isControlsOpen = false;
                break;
        }
    }

    private void SetResultUI(string message)
    {
        if (_resultPanel == null) return;
        _resultText.SetText(message);
    }

    private void HideAllPanels()
    {
        if (_allPanels == null) return;

        for (int i = 0; i < _allPanels.Length; i++)
        {
            _allPanels[i]?.SetActive(false);
        }
    }

    #region 버튼 클릭

    public void OnResumeClicked()
    {
        _isControlsOpen = false;
        GameManager.Instance.Resume();
    }

    public void OnControlsClicked()
    {
        _isControlsOpen = true;
        _controlsPanel?.SetActive(true);
    }

    public void OnControlsBackClicked()
    {
        _isControlsOpen = false;
        _controlsPanel?.SetActive(false);
    }

    public void OnRestartMissionClicked()
    {
        _isControlsOpen = false;
        GameManager.Instance.RestartCurrentStage();
    }

    public void OnReturnToTitleClicked()
    {
        _isControlsOpen = false;
        GameManager.Instance.LoadTitle();
    }

    public void OnResultClicked() => OnRestartMissionClicked();
    public void OnResultReturnToTitle() => OnReturnToTitleClicked();

    #endregion






}
