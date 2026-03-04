using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _controlsPanel;

    public void OnStartStage01()
    {
        GameManager.Instance.LoadStage01();
    }

    public void OnStartStage02()
    {
        GameManager.Instance.LoadStage02();
    }

    public void OnOpenControls()
    {
        if (_mainPanel != null) _mainPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(true);
    }

    public void OnCloseControls()
    {
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
        if (_mainPanel != null) _mainPanel.SetActive(true);
    }

    public void OnExit()
    {
        GameManager.Instance.QuitGame();
    }
}
