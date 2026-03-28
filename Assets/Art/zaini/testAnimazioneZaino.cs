using UnityEngine;

public class ZainoController : MonoBehaviour
{
    public Animator animator;  
    public string parametroAnim = "statoApertura";  

    private int stato = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            // Incrementa lo stato e torna a 0 dopo il 2
            stato = (stato + 1) % 3;

            // Aggiorna l'animator
            animator.SetInteger(parametroAnim, stato);

            Debug.Log("Stato zaino: " + stato);
        }
    }
}
