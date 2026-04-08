using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    [Header("Door Setup")]
    [SerializeField] private Transform doorTransform;

    [Header("Motion")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openCloseSpeed = 6f;
    [SerializeField] private bool openPositiveY = true;

    // Luu san goc dong/mo de moi lan tuong tac chi can noi suy den dich.
    private Quaternion closedRotation;
    private Quaternion openRotation;

    // Theo doi trang thai cua canh cua va coroutine xoay hien tai.
    private bool isOpen;
    private Coroutine rotateRoutine;

    private void Awake()
    {
        // Neu chua chi dinh canh cua rieng thi mac dinh xoay ngay object dang gan script.
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedRotation = doorTransform.localRotation;

        // Tinh truoc goc mo dua theo huong xoay mong muon.
        float signedAngle = openPositiveY ? openAngle : -openAngle;
        openRotation = closedRotation * Quaternion.Euler(0f, signedAngle, 0f);
    }

    protected override void Interact()
    {
        // Door ke thua Interactable, nen luc nguoi choi bam tuong tac chi can doi trang thai.
        Toggle();
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        // Neu cua dang xoay do dang thi dung luong cu de tranh hai coroutine cung ghi rotation.
        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
        }

        rotateRoutine = StartCoroutine(RotateTo(isOpen ? openRotation : closedRotation));
    }

    private IEnumerator RotateTo(Quaternion target)
    {
        // Slerp ket hop ham mu giup cua mo/dong mem, khong bi giat cuc.
        while (Quaternion.Angle(doorTransform.localRotation, target) > 0.1f)
        {
            doorTransform.localRotation = Quaternion.Slerp(
                doorTransform.localRotation,
                target,
                1f - Mathf.Exp(-openCloseSpeed * Time.deltaTime));
            yield return null;
        }

        // Chot dung goc dich de tranh sai so lech nho sau khi noi suy.
        doorTransform.localRotation = target;
        rotateRoutine = null;
    }
}
