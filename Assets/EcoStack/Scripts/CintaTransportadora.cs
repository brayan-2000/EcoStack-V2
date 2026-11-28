using UnityEngine;

public class CintaTransportadora : MonoBehaviour
{
    [Header("Empuje de Cinta")]
    public float fuerzaEmpuje = 4f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 empuje = transform.forward * fuerzaEmpuje * Time.deltaTime;

            // Funciona tanto si el Integrante 1 usa CharacterController como Transform/Rigidbody
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.Move(empuje);
            }
            else
            {
                other.transform.position += empuje;
            }
        }
    }
}
