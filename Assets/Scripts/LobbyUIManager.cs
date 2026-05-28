using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject panelConnect;
    [SerializeField] private GameObject panelLobby;
    [SerializeField] private GameObject panelRoom;

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

    [Header("Prefabs")]
    [SerializeField] private GameObject roomEntryPrefab;
    [SerializeField] private GameObject playerEntryPrefab; // texto simples com nome do jogador

    // Guarda os itens de jogador criados para poder remover depois
    private Dictionary<PlayerRef, GameObject> _playerEntries = new Dictionary<PlayerRef, GameObject>();

    private void Start()
    {
        // Começa mostrando só o painel de conectar
        ShowPanel(panelConnect);

        // Conecta os botões às funções
        connectButton.onClick.AddListener(OnConnectButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
    }

    // --- Botões ---

    private void OnConnectButtonClicked()
    {
        string lobbyName = lobbyNameInput.text;
        if (string.IsNullOrEmpty(lobbyName)) return;

        SetInteractable(false); // desativa botões enquanto conecta
        FusionLobbyManager.Instance.ConnectToLobby(lobbyName);
    }

    private void OnCreateRoomButtonClicked()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) return;

        // Tenta converter o texto do input pra número, usa 4 como padrão se falhar
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
        // Apaga todos os botões de sala antigos
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        playerCountText.text = "Salas disponíveis: " + sessionList.Count;

        // Cria um botão novo pra cada sala
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

    // --- Utilitários ---

    private void ShowPanel(GameObject panel)
    {
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