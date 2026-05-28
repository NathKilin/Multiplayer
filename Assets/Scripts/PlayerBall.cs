using Fusion;
using UnityEngine;

// NetworkBehaviour = versão do MonoBehaviour do Fusion,
// usada em objetos que existem na rede entre todos os jogadores
public class PlayerBall : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody _rb;

    public override void Spawned()
    {
        // Spawned() = chamado pelo Fusion quando o objeto é criado na rede,
        // equivalente ao Start() mas para objetos de rede

        _rb = GetComponent<Rigidbody>();

        // Só ativa a câmera pra bola que pertence ao jogador local
        if (HasInputAuthority)
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 10, -8);
            Camera.main.transform.LookAt(transform);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // FixedUpdateNetwork() = equivalente ao FixedUpdate() do Unity,
        // mas sincronizado com a rede — só roda pra quem tem controle do objeto

        if (!HasInputAuthority) return;

        float h = Input.GetAxisRaw("Horizontal"); // A e D
        float v = Input.GetAxisRaw("Vertical");   // W e S

        Vector3 direction = new Vector3(h, 0, v).normalized;
        _rb.linearVelocity = direction * speed;
    }
}