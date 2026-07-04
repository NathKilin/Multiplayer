using Fusion;
using UnityEngine;

// NetworkBehaviour = vers�o do MonoBehaviour do Fusion,
// usada em objetos que existem na rede entre todos os jogadores
public class PlayerBall : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody _rb;

    public override void Spawned()
    {
        // Spawned() = chamado pelo Fusion quando o objeto � criado na rede,
        // equivalente ao Start() mas para objetos de rede

        _rb = GetComponent<Rigidbody>();

        // S� ativa a c�mera pra bola que pertence ao jogador local
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
        // mas sincronizado com a rede � s� roda pra quem tem controle do objeto

        if (!HasInputAuthority) return;

        float h = UnityEngine.InputSystem.Keyboard.current != null ?
            (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed ? 1f :
             UnityEngine.InputSystem.Keyboard.current.aKey.isPressed ? -1f : 0f) : 0f;

        float v = UnityEngine.InputSystem.Keyboard.current != null ?
            (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed ? 1f :
             UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ? -1f : 0f) : 0f;

        Vector3 direction = new Vector3(h, 0, v).normalized;
        _rb.linearVelocity = direction * speed;
    }
}