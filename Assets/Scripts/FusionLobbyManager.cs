using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// INetworkRunnerCallbacks = interface (contrato) do Fusion que obriga
// esse script a ter todos os callbacks de rede implementados
public class FusionLobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Static Instance = qualquer script pode acessar esse manager sem precisar de referência
    public static FusionLobbyManager Instance { get; private set; }

    [SerializeField] private LobbyUIManager uiManager;
    [SerializeField] private NetworkPrefabRef playerBallPrefab; // prefab da bola em formato de rede

    private NetworkRunner _runner; // o componente central do Fusion que gerencia a conexão

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- Ações do jogador ---

    public async void ConnectToLobby(string lobbyName)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);

        if (result.Ok)
            uiManager.OnConnectedToLobby();
        else
            uiManager.OnOperationFailed("Falha ao conectar ao lobby");
    }

    public async void CreateRoom(string roomName, int maxPlayers)
    {
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = maxPlayers,
        });

        if (result.Ok)
            uiManager.OnJoinedRoom(roomName);
        else
            uiManager.OnOperationFailed("Falha ao criar sala");
    }

    public async void JoinRoom(string sessionName)
    {
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
        });

        if (result.Ok)
            uiManager.OnJoinedRoom(sessionName);
        else
            uiManager.OnOperationFailed("Falha ao entrar na sala");
    }

    public async void LeaveRoom()
    {
        await _runner.Shutdown();
        _runner = null;
        uiManager.OnLeftRoom();
    }

    // --- Callbacks do Fusion (chamados automaticamente pela rede) ---

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Chamado sempre que a lista de salas no lobby muda
        uiManager.UpdateRoomList(sessionList);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Chamado quando qualquer jogador entra na sala
        uiManager.AddPlayerToList(player);

        // Só spawna (cria na rede) a bola do jogador local
        if (player == runner.LocalPlayer)
        {
            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, 0f);
            runner.Spawn(playerBallPrefab, spawnPos, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Chamado quando qualquer jogador sai da sala
        uiManager.RemovePlayerFromList(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Runner encerrado: " + shutdownReason);
    }

    // Callbacks obrigatórios pela interface mas que não precisamos usar agora
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}