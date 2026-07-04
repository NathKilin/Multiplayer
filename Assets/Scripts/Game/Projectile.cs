using Fusion;
using UnityEngine;

// Proj�til simples (mec�nica de "atirar" do item 4). Anda pra frente e se destr�i
// sozinho depois de um tempo ou ao colidir com algo.
public class Projectile : NetworkBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetimeSeconds = 3f;

    private TickTimer _life;

    public override void Spawned()
    {
        _life = TickTimer.CreateFromSeconds(Runner, lifetimeSeconds);
    }

    public override void FixedUpdateNetwork()
    {
        // Quem tem StateAuthority sobre o proj�til (o Master, ver PlayerCharacter.Shoot)
        // � quem decide o movimento e quando dar Despawn - evita dois peers tentando
        // despawnar o mesmo objeto ao mesmo tempo.
        if (!Object.HasStateAuthority) return;

        transform.position += transform.forward * speed * Runner.DeltaTime;

        if (_life.Expired(Runner))
            Runner.Despawn(Object);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority) return;

        // Evita destruir duas vezes caso v�rias colis�es cheguem no mesmo tick.
        if (Object.IsValid)
            Runner.Despawn(Object);
    }
}
