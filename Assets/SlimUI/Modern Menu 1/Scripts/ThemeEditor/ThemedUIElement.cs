using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu
{
	[System.Serializable]
	public class ThemedUIElement : ThemedUI
	{
		[Header("Parameters")]
		Color outline;
		Image image;
		GameObject message;
		public enum OutlineStyle { solidThin, solidThick, dottedThin, dottedThick };
		[Tooltip("Đối tượng UI có thành phần Image để đổi màu theo theme")]
		public bool hasImage = false;
		[Tooltip("Đối tượng UI là TextMeshPro cần đổi màu chữ theo theme")]
		public bool isText = false;

		protected override void OnSkinUI()
		{
			base.OnSkinUI();

			if (hasImage)
			{
				image = GetComponent<Image>();
				image.color = themeController.currentColor;
			}

			message = gameObject;

			if (isText)
			{
				message.GetComponent<TextMeshPro>().color = themeController.textColor;
			}
		}
	}
}