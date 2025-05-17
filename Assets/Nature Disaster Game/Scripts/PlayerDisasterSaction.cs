using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector.vCharacterController;
using Invector;

public class PlayerDisasterSaction : MonoBehaviour
{
    public bool playerInTornado;
    private void OnTriggerStay(Collider other)
    {
        if(other.tag== "Player")
        {
            if(other.GetComponent<vThirdPersonController>() != null)
            {
                other.GetComponent<vThirdPersonController>().speedMultiplier = 0;
                other.GetComponent<Animator>().enabled = false;
                other.GetComponent<BodyPartsController>().DeleteRandomPart();
                //  other.GetComponent<Rigidbody>().freezeRotation = false;

                playerInTornado = true;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
        {
            Invoke("PlayerInGround", 2f);
        }
    }

    void PlayerInGround()
    {
        playerInTornado = false;
    }
}
