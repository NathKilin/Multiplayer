using Fusion;
using TMPro;
using UnityEngine;

// Personagem escolhido pelo jogador (um dos 10 prefabs, ver CharacterSelectionManager).
// Tem NetworkTransform (adicionado no prefab, no Editor) cuidando de sincronizar
// posi��o/rota��o automaticamente - esse script s� cuida de movimento, atirar,
// nickname/cor (b�nus) e chat.
public class PlayerCharacter : NetworkBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float speed = 5f;

    [Header("Atirar")]
    [SerializeField] private NetworkPrefabRef projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldownSeconds = 0.4f;

    [Header("Apar�ncia (b�nus)")]
    [SerializeField] private Renderer bodyRenderer; // renderer do capsule, pra pintar com a cor escolhida
    [SerializeField] private TextMeshPro nicknameLabel; // texto 3D flutuando acima do personagem

    private Rigidbody _rb;
    private TickTimer _fireCooldown;

    // Refer�ncia est�tica pro personagem do jogador LOCAL (o que essa m�quina controla).
    // Usado pela UI de chat pra saber em qual objeto chamar as RPCs de envio.
    public static PlayerCharacter LocalCharacter { get; private set; }

    // --- Networked (b�nus: nickname + cor) ---
    [Networked, OnChangedRender(nameof(OnAppearanceChanged))]
    public NetworkString<_16> Nickname { get; set; }

    [Networked, OnChangedRender(nameof(OnAppearanceChanged))]
    public int ColorIndex { get; set; }

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();

        // Aplica a apar�ncia j� no primeiro frame (os valores networked j� chegam
        // preenchidos por causa do onBeforeSpawned no CharacterSelectionManager).
        OnAppearanceChanged();

        if (HasInputAuthority)
        {
            LocalCharacter = this;

            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 10, -8);
            Camera.main.transform.LookAt(transform);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (LocalCharacter == this) LocalCharacter = null;
    }

    public override void FixedUpdateNetwork()
    {
        // IMPORTANTE: aqui usamos HasStateAuthority, n�o HasInputAuthority (diferente do
        // PlayerBall da Tarefa 1). Isso porque quem spawna o personagem � sempre o Master
        // Client (CharacterSelectionManager.TryClaimSlot), ent�o a StateAuthority do
        // personagem � o Master - n�o necessariamente o jogador que est� controlando ele.
        // O input do dono chega via GetInput (relay autom�tico do Fusion pela rede) e �
        // simulado aqui, na m�quina do Master; o resultado (posi��o/rota��o) volta pra
        // todo mundo, inclusive pro dono, via NetworkTransform.
        if (!Object.HasStateAuthority) return;

        if (GetInput(out GameplayInput input))
        {
            Vector3 dir = new Vector3(input.Move.x, 0f, input.Move.y);
            if (dir.sqrMagnitude > 0.01f)
            {
                _rb.linearVelocity = dir.normalized * speed;
                transform.forward = dir.normalized;
            }
            else
            {
                _rb.linearVelocity = Vector3.zero;
            }

            if (input.Fire && _fireCooldown.ExpiredOrNotRunning(Runner))
            {
                _fireCooldown = TickTimer.CreateFromSeconds(Runner, fireCooldownSeconds);
                Shoot();
            }
        }
    }

    // Mec�nica escolhida (item 4 do enunciado): atirar um proj�til que se destr�i sozinho
    // depois de um tempo (ver Projectile.cs). Quem chama Spawn aqui � sempre a StateAuthority
    // do personagem (o Master), ent�o o proj�til nasce com StateAuthority = Master tamb�m,
    // e � o Master quem decide quando dar Despawn nele.
    private void Shoot()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward;
        Runner.Spawn(projectilePrefab, spawnPos, transform.rotation, Object.InputAuthority);
    }

    private void OnAppearanceChanged()
    {
        if (bodyRenderer != null)
            bodyRenderer.material.color = PlayerColorPalette.GetColor(ColorIndex);

        if (nicknameLabel != null)
            nicknameLabel.text = Nickname.ToString();
    }

    // --- Chat (item 5 do enunciado) ---
    // Chamados a partir do PlayerCharacter.LocalCharacter (ver ChatUIManager), ou seja,
    // sempre no PR�PRIO personagem de quem est� enviando - por isso RPC_SendPrivateMessage
    // pode usar RpcSources.InputAuthority (s� o dono desse objeto pode chamar).

    // Chat global: qualquer cliente pode chamar (RpcSources.All), todo mundo recebe.
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendGlobalMessage(string senderName, string message)
    {
        ChatUIManager.Instance?.AddGlobalMessage(senderName, message);
    }

    // Chat privado: [RpcTarget] faz esse m�todo s� RODAR na m�quina do targetPlayer.
    // N�o precisa comparar Runner.LocalPlayer manualmente - o Fusion j� filtra isso.
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendPrivateMessage([RpcTarget] PlayerRef targetPlayer, string senderName, string message)
    {
        ChatUIManager.Instance?.AddPrivateMessage(senderName, message);
    }
}
