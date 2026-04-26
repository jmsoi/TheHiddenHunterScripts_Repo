using Unity.Netcode;
using UnityEngine;

public class Landmine : MonoBehaviour
{
    private PlayerCombat ownerCombat;
    private ulong ownerClientId;
    private bool hasDetonated;

    public ulong OwnerClientId => ownerClientId;

    public void Initialize(PlayerCombat combat)
    {
        ownerCombat = combat;
        ownerClientId = combat.OwnerClientId;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDetonate(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDetonate(collision != null ? collision.gameObject : null);
    }

    private void TryDetonate(GameObject hitObject)
    {
        if (hasDetonated || ownerCombat == null || hitObject == null)
            return;
        // LandmineDetonateServerRpc는 설치자(PlayerCombat) 오너만 호출 가능(RequireOwnership)
        if (!ownerCombat.IsOwner)
            return;

        var hitNetworkObject = hitObject.GetComponentInParent<NetworkObject>();
        if (hitNetworkObject == null)
            return;

        if (hitNetworkObject.OwnerClientId == ownerClientId)
            return;

        var root = hitNetworkObject.gameObject;
        if (root.GetComponent<PlayerHealth>() == null && root.GetComponent<NPCController>() == null)
            return;

        hasDetonated = true;

        ownerCombat.LandmineDetonateServerRpc(hitNetworkObject.NetworkObjectId);

        Destroy(gameObject);
    }
}
