using UnityEngine;
using UnityEngine.Video;

public class CutSceneCrouchTrigger : MonoBehaviour
{
    public GameObject CutSceneObject;
    public Animator cutSceneAnim;
    public bool preRendered;
    public VideoPlayer preRenderedVideo;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (preRendered)
            {
                CutSceneObject.SetActive(true);
            }
            else
            {
                cutSceneAnim.SetTrigger("PlayCutScene");
            }
        }
    }
}
