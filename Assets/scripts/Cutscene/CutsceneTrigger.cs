using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene")]
    [Tooltip("GameObject cutscene sẽ được bật khi player đi vào trigger")]
    public GameObject cutscene;
    [Tooltip("Animator điều khiển cutscene")]
    public Animator cutsceAnim;
    [Tooltip("Bật nếu dùng video prerender thay cho animation realtime")]
    public bool preRendered;
    [Tooltip("VideoPlayer phát cutscene prerender")]
    public VideoPlayer preRenderedPlayer;
    [Tooltip("Tên trigger parameter trong Animator để chạy cutscene")]
    public string cutsceneTriggerName;

    [Header("Player")]
    [Tooltip("Script điều khiển di chuyển player sẽ bị tắt trong cutscene")]
    public PlayerMovement playerScript;
    [Tooltip("Camera gameplay của player")]
    public Camera playerCamera;
    [Tooltip("Thời lượng cutscene (giây) trước khi trả quyền điều khiển")]
    public float cutsceneDuration;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerScript.enabled = false;
            if (preRendered == false)
            {
                cutscene.SetActive(true);
                playerCamera.enabled = false;
                cutsceAnim.SetTrigger("cutsceneTriggerName");
                StartCoroutine(WaitForCutscene());
            }
            this.gameObject.GetComponent<BoxCollider>().enabled = false;

        }

    }
    IEnumerator WaitForCutscene()
    {
        yield return new WaitForSeconds(cutsceneDuration);
        playerScript.enabled = true;
        playerCamera.enabled = true;
    }
}