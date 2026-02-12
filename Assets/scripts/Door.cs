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

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;
    private Coroutine rotateRoutine;

    private void Awake()
    {
        if (doorTransform == null) doorTransform = transform;
        closedRotation = doorTransform.localRotation;
        float signedAngle = openPositiveY ? openAngle : -openAngle;
        openRotation = closedRotation * Quaternion.Euler(0f, signedAngle, 0f);
    }

    protected override void Interact()
    {
        Toggle();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        if (rotateRoutine != null) StopCoroutine(rotateRoutine);
        rotateRoutine = StartCoroutine(RotateTo(isOpen ? openRotation : closedRotation));
    }

    private IEnumerator RotateTo(Quaternion target)
    {
        // Xoay mượt mà bằng cách sử dụng làm mịn hàm mũ hướng tới mục tiêu.
        while (Quaternion.Angle(doorTransform.localRotation, target) > 0.1f)
        {
            doorTransform.localRotation = Quaternion.Slerp(
                doorTransform.localRotation,
                target,
                1f - Mathf.Exp(-openCloseSpeed * Time.deltaTime)
            );
            yield return null;
        }

        doorTransform.localRotation = target;
        rotateRoutine = null;
    }
}

