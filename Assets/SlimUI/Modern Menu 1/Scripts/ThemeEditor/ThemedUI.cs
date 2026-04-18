using UnityEngine;

namespace SlimUI.ModernMenu
{
	[ExecuteInEditMode()]
	[System.Serializable]
	public class ThemedUI : MonoBehaviour
	{

		[Tooltip("Dữ liệu theme dùng để tô màu UI")]
		public ThemedUIData themeController;

		protected virtual void OnSkinUI()
		{

		}

		public virtual void Awake()
		{
			OnSkinUI();
		}

		public virtual void Update()
		{
			OnSkinUI();
		}
	}
}
