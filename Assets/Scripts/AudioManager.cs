using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip audioPuckHit;
    public AudioClip audioWallHit;
    public AudioClip audioGoal;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Pusher")
            audioSource.PlayOneShot(audioPuckHit);

        else if (collision.gameObject.tag == "Table")
            audioSource.PlayOneShot(audioWallHit);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
            audioSource.PlayOneShot(audioGoal);
    }
}
