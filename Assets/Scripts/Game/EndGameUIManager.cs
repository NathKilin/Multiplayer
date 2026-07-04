using UnityEngine;
using UnityEngine.UI;

// Item 6 do enunciado: encerrar a partida. O bot�o "Encerrar Jogo" s� aparece pro
// Master Client; ao ser clicado, dispara um RPC (CharacterSelectionManager.RPC_EndGame)
// que abre o mesmo menu de fim de jogo em TODOS os clientes.
public class EndGameUIManager : MonoBehaviour
{
    public static EndGameUIManager Instance { get; private set; }

    [Header("S� aparece pro Master Client")]
    [SerializeField] private Button endGameButton;

    [Header("Menu de fim de jogo (aparece em todos os clientes)")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private Button leaveButton;

    private void Awake()
    {
        Instance = this;

        if (endGamePanel != null) endGamePanel.SetActive(false);

        endGameButton.onClick.AddListener(OnEndGameButtonClicked);
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
    }

    private void Start()
    {
        // S� o Master Client pode encerrar a partida (mesma regra do bot�o "Iniciar Partida").
        bool isMaster = FusionLobbyManager.Instance != null && FusionLobbyManager.Instance.IsMasterClient;
        endGameButton.gameObject.SetActive(isMaster);
    }

    private void OnEndGameButtonClicked()
    {
        CharacterSelectionManager.Instance?.RPC_EndGame();
    }

    // Chamado pelo RPC_EndGame em TODOS os clientes (inclusive no Master, gra�as ao InvokeLocal padr�o do Fusion).
    public void ShowEndGameMenu()
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);
    }

    private void OnLeaveButtonClicked()
    {
        // Cada cliente fecha a pr�pria sess�o e volta pro menu principal (cena de Lobby).
        FusionLobbyManager.Instance.ReturnToLobby();
    }
}
