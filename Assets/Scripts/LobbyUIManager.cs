using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Pain�is")]
    [SerializeField] private GameObject panelProfile; // Tarefa 2 (b�nus): escolha de nickname + cor, antes de tudo
    [SerializeField] private GameObject panelConnect;
    [SerializeField] private GameObject panelLobby;
    [SerializeField] private GameObject panelRoom;

    [Header("PanelProfile (b�nus: nickname + cor)")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button[] colorButtons; // um bot�o por cor da PlayerColorPalette, na mesma ordem
    [SerializeField] private Button profileConfirmButton;

    [Header("PanelConnect")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Button connectButton;

    [Header("PanelLobby")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Transform roomListContent; // o Content dentro do ScrollView das salas

    [Header("PanelRoom")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private Transform playerListContent; // o Content dentro do ScrollView dos jogadores
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startMatchButton; // Tarefa 2: s� habilitado pro Master Client

    [Header("Prefabs")]
    [SerializeField] private GameObject roomEntryPrefab;
    [SerializeField] private GameObject playerEntryPrefab; // texto simples com nome do jogador

    // Guarda os itens de jogador criados para poder remover depois
    private Dictionary<PlayerRef, GameObject> _playerEntries = new Dictionary<PlayerRef, GameObject>();

    private int _selectedColorIndex = 0;

    private void Start()
    {
        // Alterado (Tarefa 2): o FusionLobbyManager agora sobrevive � troca de cena
        // (DontDestroyOnLoad), ent�o quando a cena de Lobby recarrega (ex: depois de um
        // "Encerrar Jogo") essa � uma inst�ncia NOVA de LobbyUIManager - precisa se
        // reapresentar pro manager persistente pra n�o ficar com refer�ncia nula.
        FusionLobbyManager.Instance?.SetUIManager(this);

        // Come�a mostrando o painel de perfil (b�nus: nickname + cor)
        ShowPanel(panelProfile);

        // Conecta os bot�es �s fun��es
        profileConfirmButton.onClick.AddListener(OnProfileConfirmClicked);
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int colorIndex = i; // copia local pra n�o vazar a vari�vel de loop pro closure
            colorButtons[i].onClick.AddListener(() => _selectedColorIndex = colorIndex);
        }

        connectButton.onClick.AddListener(OnConnectButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        startMatchButton.onClick.AddListener(OnStartMatchButtonClicked);
    }

    // --- Bot�es ---

    private void OnProfileConfirmClicked()
    {
        string nickname = nicknameInput.text;
        LocalPlayerSettings.Nickname = string.IsNullOrEmpty(nickname) ? "Jogador" : nickname;
        LocalPlayerSettings.ColorIndex = _selectedColorIndex;

        ShowPanel(panelConnect);
    }

    private void OnConnectButtonClicked()
    {
        string lobbyName = lobbyNameInput.text;
        if (string.IsNullOrEmpty(lobbyName)) return;

        SetInteractable(false); // desativa bot�es enquanto conecta
        FusionLobbyManager.Instance.ConnectToLobby(lobbyName);
    }

    private void OnCreateRoomButtonClicked()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) return;

        // Tenta converter o texto do input pra n�mero, usa 4 como padr�o se falhar
        if (!int.TryParse(maxPlayersInput.text, out int maxPlayers))
            maxPlayers = 4;

        SetInteractable(false);
        FusionLobbyManager.Instance.CreateRoom(roomName, maxPlayers);
    }

    private void OnLeaveButtonClicked()
    {
        SetInteractable(false);
        FusionLobbyManager.Instance.LeaveRoom();
    }

    // Tarefa 2: dispara a troca de cena pra GameScene. S� o Master Client v� esse
    // bot�o interativo (ver OnJoinedRoom), mas o pr�prio FusionLobbyManager.StartMatch
    // tamb�m confere de novo antes de agir.
    private void OnStartMatchButtonClicked()
    {
        FusionLobbyManager.Instance.StartMatch();
    }

    // --- Chamados pelo FusionLobbyManager ---

    public void OnConnectedToLobby()
    {
        ShowPanel(panelLobby);
        SetInteractable(true);
    }

    public void OnJoinedRoom(string roomName)
    {
        roomNameText.text = "Sala: " + roomName;
        ShowPanel(panelRoom);
        SetInteractable(true);

        // Tarefa 2: s� o Master Client pode iniciar a partida (mesma regra do bot�o de
        // encerrar jogo, item 6 do enunciado).
        startMatchButton.interactable = FusionLobbyManager.Instance.IsMasterClient;
    }

    public void OnLeftRoom()
    {
        // Limpa a lista de jogadores ao sair
        foreach (var entry in _playerEntries.Values)
            Destroy(entry);
        _playerEntries.Clear();

        ShowPanel(panelLobby);
        SetInteractable(true);
    }

    public void OnOperationFailed(string message)
    {
        Debug.LogError("Erro: " + message);
        SetInteractable(true);
    }

    public void UpdateRoomList(List<SessionInfo> sessionList)
    {
        // Apaga todos os bot�es de sala antigos
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        playerCountText.text = "Salas dispon�veis: " + sessionList.Count;

        // Cria um bot�o novo pra cada sala
        foreach (var session in sessionList)
        {
            GameObject entry = Instantiate(roomEntryPrefab, roomListContent);
            entry.GetComponent<RoomEntry>().Setup(session.Name, session.PlayerCount, session.MaxPlayers);
        }
    }

    public void AddPlayerToList(PlayerRef player)
    {
        if (_playerEntries.ContainsKey(player)) return;

        GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
        entry.GetComponentInChildren<TextMeshProUGUI>().text = "Jogador " + player.PlayerId;
        _playerEntries[player] = entry;
    }

    public void RemovePlayerFromList(PlayerRef player)
    {
        if (_playerEntries.TryGetValue(player, out GameObject entry))
        {
            Destroy(entry);
            _playerEntries.Remove(player);
        }
    }

    // --- Utilit�rios ---

    private void ShowPanel(GameObject panel)
    {
        panelProfile.SetActive(false);
        panelConnect.SetActive(false);
        panelLobby.SetActive(false);
        panelRoom.SetActive(false);
        panel.SetActive(true);
    }

    private void SetInteractable(bool state)
    {
        connectButton.interactable = state;
        createRoomButton.interactable = state;
        leaveButton.interactable = state;
    }
}