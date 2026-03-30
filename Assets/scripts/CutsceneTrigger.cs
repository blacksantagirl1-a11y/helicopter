using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneTrigger : MonoBehaviour
{
    public GameObject cutscene;
    public Animator cutsceAnim;
    public bool preRendered;
    public VideoPlayer preRenderedPlayer;
    public string cutsceneTriggerName;
    public FirstPersonMovement playerScript;
    public Camera playerCamera;
    public float cutsceneDuration;

    void OnTriggerEnter(Collider other){
        if(other.CompareTag("Player"))
        {
            playerScript.enabled = false;
            if(preRendered == false)
            {
                cutscene.SetActive(true);
                playerCamera.enabled = false;
                cutsceAnim.SetTrigger("cutsceneTriggerName");
                StartCoroutine(WaitForCutscene());
            }
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
    
        }
    
}
IEnumerator WaitForCutscene(){
    yield return new WaitForSeconds(cutsceneDuration);
    playerScript.enabled = true;
    playerCamera.enabled = true;
}
}