using UnityEngine;

namespace SlimUI.ModernMenu
{
	[CreateAssetMenu(menuName = "ThemeSettings")]
	[System.Serializable]
	public class ThemedUIData : ScriptableObject
	{
		[System.Serializable]
		public class Custom1
		{
			[Header("Text")]
			[Tooltip("Màu đồ họa chính của preset 1")]
			public Color graphic1;
			[Tooltip("Màu chữ của preset 1")]
			public Color32 text1;
		}

		[System.Serializable]
		public class Custom2
		{
			[Header("Text")]
			[Tooltip("Màu đồ họa chính của preset 2")]
			public Color graphic2;
			[Tooltip("Màu chữ của preset 2")]
			public Color32 text2;
		}

		[System.Serializable]
		public class Custom3
		{
			[Header("Text")]
			[Tooltip("Màu đồ họa chính của preset 3")]
			public Color graphic3;
			[Tooltip("Màu chữ của preset 3")]
			public Color32 text3;
		}

		[Header("PRESETS")]
		[Tooltip("Thiết lập màu cho preset giao diện 1")]
		public Custom1 custom1;
		[Tooltip("Thiết lập màu cho preset giao diện 2")]
		public Custom2 custom2;
		[Tooltip("Thiết lập màu cho preset giao diện 3")]
		public Custom3 custom3;

		[HideInInspector]
		public Color currentColor;
		[HideInInspector]
		public Color32 textColor;
	}
}