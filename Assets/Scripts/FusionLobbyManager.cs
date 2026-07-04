using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// INetworkRunnerCallbacks = interface (contrato) do Fusion que obriga
// esse script a ter todos os callbacks de rede implementados
public class FusionLobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Static Instance = qualquer script pode acessar esse manager sem precisar de refer�ncia
    public static FusionLobbyManager Instance { get; private set; }

    [SerializeField] private LobbyUIManager uiManager;
    [SerializeField] private NetworkPrefabRef playerBallPrefab; // prefab da bola em formato de rede

    // --- Tarefa 2: troca de cena via Fusion ---
    // Nome e build index da cena de jogo (Assets > Scenes > GameScene).
    // O nome � usado s� pra comparar a cena ativa, o build index � o que o Fusion realmente usa pra carregar.
    // IMPORTANTE: adicione a GameScene em File > Build Settings e ajuste esse index se a posi��o dela mudar.
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private int gameSceneBuildIndex = 1;
    [SerializeField] private string lobbySceneName = "Lobby"; // nome exato da cena de lobby (a mesma da Tarefa 1)

    private NetworkRunner _runner; // o componente central do Fusion que gerencia a conex�o

    // Exp�e o runner e o status de "master" pra outros scripts da Tarefa 2 (sele��o de personagem, chat, etc.)
    public NetworkRunner Runner => _runner;
    public bool IsMasterClient => _runner != null && _runner.IsSharedModeMasterClient;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Alterado (Tarefa 2): esse objeto agora sobrevive � troca de cena (DontDestroyOnLoad,
    // ver ConnectToLobby). Isso significa que quando a gente volta pra cena de Lobby (fim de
    // jogo -> "Sair"), uma NOVA inst�ncia de LobbyUIManager � criada, mas o FusionLobbyManager
    // continua sendo o mesmo de antes, com a refer�ncia antiga (destru�da) pro uiManager.
    // O LobbyUIManager novo chama isso no pr�prio Start() pra se reconectar.
    public void SetUIManager(LobbyUIManager manager)
    {
        uiManager = manager;
    }

    // --- A��es do jogador ---

    public async void ConnectToLobby(string lobbyName)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // Alterado (Tarefa 2): precisamos de um scene manager pro Fusion poder trocar
        // de cena (lobby -> jogo) de forma sincronizada entre todos os clientes.
        if (gameObject.GetComponent<NetworkSceneManagerDefault>() == null)
            gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Alterado (Tarefa 2): esse GameObject carrega o NetworkRunner, ent�o ele
        // precisa sobreviver � troca de cena pra n�o perder a conex�o nem esse manager.
        DontDestroyOnLoad(gameObject);

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

    // Tarefa 2: chamado pelo bot�o "Iniciar Partida", s� dispon�vel pro Master Client.
    // Usa o sistema de troca de cena do Fusion (NetworkSceneManagerDefault), que sincroniza
    // automaticamente a troca de cena pra todos os peers conectados.
    public void StartMatch()
    {
        if (!IsMasterClient)
        {
            Debug.LogWarning("Apenas o Master Client pode iniciar a partida.");
            return;
        }

        _runner.LoadScene(SceneRef.FromIndex(gameSceneBuildIndex), LoadSceneMode.Single, LocalPhysicsMode.None, true);
    }

    // Tarefa 2, item 6: chamado pelo bot�o "Sair" do menu de fim de jogo, em TODOS os clientes
    // (cada m�quina fecha a pr�pria sess�o local e volta pro menu principal). Depois do
    // Shutdown n�o tem mais rede envolvida, ent�o um SceneManager.LoadScene comum resolve.
    public async void ReturnToLobby()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
            _runner = null;
        }

        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    // --- Callbacks do Fusion (chamados automaticamente pela rede) ---

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Chamado sempre que a lista de salas no lobby muda
        uiManager.UpdateRoomList(sessionList);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Alterado (Tarefa 2): esse callback agora atende duas cenas diferentes.
        // Na cena de lobby o comportamento original (spawnar a bola) continua igual.
        // Na GameScene, um jogador entrando (inclusive entrando atrasado, depois da
        // partida j� ter come�ado) precisa ganhar um PlayerSession pra poder pedir personagem.
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (IsMasterClient)
                GameSessionBootstrap.Instance?.SpawnPlayerSession(player);
            return;
        }

        // Chamado quando qualquer jogador entra na sala
        uiManager.AddPlayerToList(player);

        // S� spawna (cria na rede) a bola do jogador local
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

    // Callbacks obrigat�rios pela interface mas que n�o precisamos usar agora
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    // Tarefa 2: coleta o input local (WASD + Espa�o) e entrega pro Fusion todo tick.
    // Isso � necess�rio porque o personagem em jogo pode estar sendo simulado numa
    // m�quina diferente da do jogador (ver coment�rio em PlayerCharacter.FixedUpdateNetwork).
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new GameplayInput();

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            float h = kb.dKey.isPressed ? 1f : kb.aKey.isPressed ? -1f : 0f;
            float v = kb.wKey.isPressed ? 1f : kb.sKey.isPressed ? -1f : 0f;
            data.Move = new Vector2(h, v);
            data.Fire = kb.spaceKey.isPressed;
        }

        input.Set(data);
    }
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