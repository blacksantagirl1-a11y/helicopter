using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUI: MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI PickUpText;

    public void Start()
    {
        
    }
    public void UpdateText(string pickUpMessage)
    {
        if (PickUpText != null)
        {
            PickUpText.text = pickUpMessage;
            // Ẩn/hiện UI dựa trên nội dung
            PickUpText.gameObject.SetActive(!string.IsNullOrEmpty(pickUpMessage));
        }
    }
}
