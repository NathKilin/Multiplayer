using Fusion;
using UnityEngine;

// Objeto �nico (singleton) da GameScene, spawnado s� pelo Master Client
// (ver GameSessionBootstrap). Como ele s� � spawnado pelo Master, o Master
// automaticamente vira a StateAuthority dele - � isso que faz esse objeto
// funcionar como o "�rbitro" central da sele��o de personagem em Shared Mode.
public class CharacterSelectionManager : NetworkBehaviour
{
    public static CharacterSelectionManager Instance { get; private set; }

    [Header("Prefabs dos 10 personagens (index = slot, 0 a 9)")]
    [SerializeField] private NetworkPrefabRef[] characterPrefabs = new NetworkPrefabRef[10];

    [Header("Pontos de spawn (opcional, um por slot)")]
    [SerializeField] private Transform[] spawnPoints = new Transform[10];

    // Array networked: slot ocupado = index, valor = PlayerRef dono (ou PlayerRef.None se livre).
    // OnChangedRender dispara em TODOS os clientes sempre que qualquer posi��o do array muda,
    // sem precisar de polling em Update().
    [Networked, Capacity(10), OnChangedRender(nameof(OnSlotsChanged))]
    public NetworkArray<PlayerRef> Slots => default;

    // S� existe (com valores v�lidos) na m�quina do Master - guarda refer�ncia do personagem
    // spawnado em cada slot pra poder dar Despawn quando o dono liberar o slot.
    private NetworkObject[] _spawnedCharacters = new NetworkObject[10];

    public override void Spawned()
    {
        Instance = this;
        OnSlotsChanged(); // atualiza a UI assim que ela existir, com o estado inicial (tudo livre)
    }

    // Chamado pelo PlayerSession.RPC_RequestCharacter, que s� executa na m�quina do Master.
    // Por isso o guard HasStateAuthority abaixo � s� uma garantia extra, n�o a defesa principal.
    public void TryClaimSlot(int slotIndex, PlayerRef requester, NetworkString<_16> nickname, int colorIndex)
    {
        if (!Object.HasStateAuthority) return;
        if (slotIndex < 0 || slotIndex >= Slots.Length) return;

        if (Slots.Get(slotIndex) != PlayerRef.None)
        {
            // Slot ocupado - condi��o de corrida entre dois jogadores acontece aqui:
            // como TryClaimSlot s� roda numa �nica m�quina (o Master), as chamadas
            // chegam uma de cada vez e s� a primeira consegue o slot.
            RPC_DenyCharacterRequest(requester, slotIndex);
            return;
        }

        // Libera um slot anterior do mesmo jogador, se ele j� tinha escolhido outro personagem.
        ReleaseSlotOf(requester);

        Vector3 spawnPos = (spawnPoints != null && slotIndex < spawnPoints.Length && spawnPoints[slotIndex] != null)
            ? spawnPoints[slotIndex].position
            : new Vector3(slotIndex * 2.5f, 0f, 0f);

        NetworkObject character = Runner.Spawn(characterPrefabs[slotIndex], spawnPos, Quaternion.identity, requester,
            (runner, obj) =>
            {
                // onBeforeSpawned: seta os valores networked ANTES do objeto ser registrado,
                // assim todo mundo j� recebe o nickname/cor corretos desde o primeiro tick.
                var controller = obj.GetComponent<PlayerCharacter>();
                controller.Nickname = nickname;
                controller.ColorIndex = colorIndex;
            });

        _spawnedCharacters[slotIndex] = character;
        Slots.Set(slotIndex, requester);
    }

    private void ReleaseSlotOf(PlayerRef player)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots.Get(i) == player)
            {
                if (_spawnedCharacters[i] != null)
                {
                    Runner.Despawn(_spawnedCharacters[i]);
                    _spawnedCharacters[i] = null;
                }
                Slots.Set(i, PlayerRef.None);
            }
        }
    }

    // RPC direcionado (RpcTarget): s� roda na m�quina do jogador que pediu o slot ocupado.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DenyCharacterRequest([RpcTarget] PlayerRef target, int slotIndex)
    {
        CharacterSelectUI.Instance?.OnRequestDenied(slotIndex);
    }

    // Tarefa 2, item 6: encerrar a partida. S� o Master Client tem StateAuthority sobre
    // esse objeto, ent�o s� ele consegue de fato disparar esse RPC (a UI tamb�m j� esconde
    // o bot�o pra quem n�o � master, ver EndGameUIManager).
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndGame()
    {
        EndGameUIManager.Instance?.ShowEndGameMenu();
    }

    private void OnSlotsChanged()
    {
        CharacterSelectUI.Instance?.Refresh(Slots);
    }
}
