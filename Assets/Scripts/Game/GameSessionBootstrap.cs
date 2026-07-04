using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Coloque esse script num GameObject vazio na GameScene (fora de qualquer prefab de rede).
// Ele s� faz uma coisa: quando a cena carrega, se essa m�quina for o Master Client, spawna
// o CharacterSelectionManager (singleton da sala) e um PlayerSession pra cada jogador j�
// conectado. Jogadores que entram depois (late join, ap�s a partida j� ter come�ado) s�o
// tratados pelo FusionLobbyManager.OnPlayerJoined, que chama SpawnPlayerSession direto.
public class GameSessionBootstrap : MonoBehaviour
{
    public static GameSessionBootstrap Instance { get; private set; }

    [SerializeField] private NetworkPrefabRef characterSelectionManagerPrefab;
    [SerializeField] private NetworkPrefabRef playerSessionPrefab;

    // S� precisa existir na m�quina do Master (� a �nica que efetivamente spawna sess�es).
    private readonly HashSet<PlayerRef> _sessionsSpawned = new HashSet<PlayerRef>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        var runner = FusionLobbyManager.Instance != null ? FusionLobbyManager.Instance.Runner : null;
        if (runner == null || !runner.IsSharedModeMasterClient) return;

        runner.Spawn(characterSelectionManagerPrefab, Vector3.zero, Quaternion.identity);

        foreach (var player in runner.ActivePlayers)
            SpawnPlayerSession(player);
    }

    public void SpawnPlayerSession(PlayerRef player)
    {
        if (!_sessionsSpawned.Add(player)) return; // esse jogador j� tem sess�o

        var runner = FusionLobbyManager.Instance.Runner;
        runner.Spawn(playerSessionPrefab, Vector3.zero, Quaternion.identity, player);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
