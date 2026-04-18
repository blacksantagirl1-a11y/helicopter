using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu
{
	public class UISettingsManager : MonoBehaviour
	{

		public enum Platform { Desktop, Mobile };
		[Tooltip("Nền tảng áp dụng cấu hình: Desktop hoặc Mobile")]
		public Platform platform;
		// toggle buttons
		[Header("MOBILE SETTINGS")]
		[Tooltip("Nhãn trạng thái bật/tắt hiệu ứng âm thanh trên mobile")]
		public GameObject mobileSFXtext;
		[Tooltip("Nhãn trạng thái bật/tắt nhạc nền trên mobile")]
		public GameObject mobileMusictext;
		[Tooltip("Vạch chọn mức đổ bóng tắt trên mobile")]
		public GameObject mobileShadowofftextLINE;
		[Tooltip("Vạch chọn mức đổ bóng thấp trên mobile")]
		public GameObject mobileShadowlowtextLINE;
		[Tooltip("Vạch chọn mức đổ bóng cao trên mobile")]
		public GameObject mobileShadowhightextLINE;

		[Header("VIDEO SETTINGS")]
		[Tooltip("Nhãn trạng thái chế độ toàn màn hình")]
		public GameObject fullscreentext;
		[Tooltip("Nhãn trạng thái bật/tắt ambient occlusion")]
		public GameObject ambientocclusiontext;
		[Tooltip("Vạch chọn mức đổ bóng tắt")]
		public GameObject shadowofftextLINE;
		[Tooltip("Vạch chọn mức đổ bóng thấp")]
		public GameObject shadowlowtextLINE;
		[Tooltip("Vạch chọn mức đổ bóng cao")]
		public GameObject shadowhightextLINE;
		[Tooltip("Vạch chọn tắt khử răng cưa")]
		public GameObject aaofftextLINE;
		[Tooltip("Vạch chọn khử răng cưa 2x")]
		public GameObject aa2xtextLINE;
		[Tooltip("Vạch chọn khử răng cưa 4x")]
		public GameObject aa4xtextLINE;
		[Tooltip("Vạch chọn khử răng cưa 8x")]
		public GameObject aa8xtextLINE;
		[Tooltip("Nhãn trạng thái bật/tắt VSync")]
		public GameObject vsynctext;
		[Tooltip("Nhãn trạng thái bật/tắt motion blur")]
		public GameObject motionblurtext;
		[Tooltip("Vạch chọn chất lượng texture thấp")]
		public GameObject texturelowtextLINE;
		[Tooltip("Vạch chọn chất lượng texture trung bình")]
		public GameObject texturemedtextLINE;
		[Tooltip("Vạch chọn chất lượng texture cao")]
		public GameObject texturehightextLINE;
		[Tooltip("Nhãn trạng thái bật/tắt hiệu ứng camera")]
		public GameObject cameraeffectstext;

		[Header("GAME SETTINGS")]
		[Tooltip("Nhãn trạng thái bật/tắt HUD")]
		public GameObject showhudtext;
		[Tooltip("Nhãn trạng thái bật/tắt tooltip hướng dẫn")]
		public GameObject tooltipstext;
		[Tooltip("Nhãn độ khó thường")]
		public GameObject difficultynormaltext;
		[Tooltip("Vạch chọn độ khó thường")]
		public GameObject difficultynormaltextLINE;
		[Tooltip("Nhãn độ khó hardcore")]
		public GameObject difficultyhardcoretext;
		[Tooltip("Vạch chọn độ khó hardcore")]
		public GameObject difficultyhardcoretextLINE;

		[Header("CONTROLS SETTINGS")]
		[Tooltip("Nhãn trạng thái đảo trục chuột")]
		public GameObject invertmousetext;

		// sliders
		[Tooltip("Slider âm lượng nhạc nền")]
		public GameObject musicSlider;
		[Tooltip("Slider độ nhạy chuột trục X")]
		public GameObject sensitivityXSlider;
		[Tooltip("Slider độ nhạy chuột trục Y")]
		public GameObject sensitivityYSlider;
		[Tooltip("Slider độ mượt chuột")]
		public GameObject mouseSmoothSlider;

		private float sliderValue = 0.0f;
		private float sliderValueXSensitivity = 0.0f;
		private float sliderValueYSensitivity = 0.0f;
		private float sliderValueSmoothing = 0.0f;


		public void Start()
		{
			// check difficulty
			if (PlayerPrefs.GetInt("NormalDifficulty") == 1)
			{
				difficultynormaltextLINE.gameObject.SetActive(true);
				difficultyhardcoretextLINE.gameObject.SetActive(false);
			}
			else
			{
				difficultyhardcoretextLINE.gameObject.SetActive(true);
				difficultynormaltextLINE.gameObject.SetActive(false);
			}

			// check slider values
			musicSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("MusicVolume");
			sensitivityXSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("XSensitivity");
			sensitivityYSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("YSensitivity");
			mouseSmoothSlider.GetComponent<Slider>().value = PlayerPrefs.GetFloat("MouseSmoothing");

			// check full screen
			if (Screen.fullScreen == true)
			{
				fullscreentext.GetComponent<TMP_Text>().text = "on";
			}
			else if (Screen.fullScreen == false)
			{
				fullscreentext.GetComponent<TMP_Text>().text = "off";
			}

			// check hud value
			if (PlayerPrefs.GetInt("ShowHUD") == 0)
			{
				showhudtext.GetComponent<TMP_Text>().text = "off";
			}
			else
			{
				showhudtext.GetComponent<TMP_Text>().text = "on";
			}

			// check tool tip value
			if (PlayerPrefs.GetInt("ToolTips") == 0)
			{
				tooltipstext.GetComponent<TMP_Text>().text = "off";
			}
			else
			{
				tooltipstext.GetComponent<TMP_Text>().text = "on";
			}

			// check shadow distance/enabled
			if (platform == Platform.Desktop)
			{
				if (PlayerPrefs.GetInt("Shadows") == 0)
				{
					QualitySettings.shadowCascades = 0;
					QualitySettings.shadowDistance = 0;
					shadowofftextLINE.gameObject.SetActive(true);
					shadowlowtextLINE.gameObject.SetActive(false);
					shadowhightextLINE.gameObject.SetActive(false);
				}
				else if (PlayerPrefs.GetInt("Shadows") == 1)
				{
					QualitySettings.shadowCascades = 2;
					QualitySettings.shadowDistance = 75;
					shadowofftextLINE.gameObject.SetActive(false);
					shadowlowtextLINE.gameObject.SetActive(true);
					shadowhightextLINE.gameObject.SetActive(false);
				}
				else if (PlayerPrefs.GetInt("Shadows") == 2)
				{
					QualitySettings.shadowCascades = 4;
					QualitySettings.shadowDistance = 500;
					shadowofftextLINE.gameObject.SetActive(false);
					shadowlowtextLINE.gameObject.SetActive(false);
					shadowhightextLINE.gameObject.SetActive(true);
				}
			}
			else if (platform == Platform.Mobile)
			{
				if (PlayerPrefs.GetInt("MobileShadows") == 0)
				{
					QualitySettings.shadowCascades = 0;
					QualitySettings.shadowDistance = 0;
					mobileShadowofftextLINE.gameObject.SetActive(true);
					mobileShadowlowtextLINE.gameObject.SetActive(false);
					mobileShadowhightextLINE.gameObject.SetActive(false);
				}
				else if (PlayerPrefs.GetInt("MobileShadows") == 1)
				{
					QualitySettings.shadowCascades = 2;
					QualitySettings.shadowDistance = 75;
					mobileShadowofftextLINE.gameObject.SetActive(false);
					mobileShadowlowtextLINE.gameObject.SetActive(true);
					mobileShadowhightextLINE.gameObject.SetActive(false);
				}
				else if (PlayerPrefs.GetInt("MobileShadows") == 2)
				{
					QualitySettings.shadowCascades = 4;
					QualitySettings.shadowDistance = 100;
					mobileShadowofftextLINE.gameObject.SetActive(false);
					mobileShadowlowtextLINE.gameObject.SetActive(false);
					mobileShadowhightextLINE.gameObject.SetActive(true);
				}
			}


			// check vsync
			if (QualitySettings.vSyncCount == 0)
			{
				vsynctext.GetComponent<TMP_Text>().text = "off";
			}
			else if (QualitySettings.vSyncCount == 1)
			{
				vsynctext.GetComponent<TMP_Text>().text = "on";
			}

			// check mouse inverse
			if (PlayerPrefs.GetInt("Inverted") == 0)
			{
				invertmousetext.GetComponent<TMP_Text>().text = "off";
			}
			else if (PlayerPrefs.GetInt("Inverted") == 1)
			{
				invertmousetext.GetComponent<TMP_Text>().text = "on";
			}

			// check motion blur
			if (PlayerPrefs.GetInt("MotionBlur") == 0)
			{
				motionblurtext.GetComponent<TMP_Text>().text = "off";
			}
			else if (PlayerPrefs.GetInt("MotionBlur") == 1)
			{
				motionblurtext.GetComponent<TMP_Text>().text = "on";
			}

			// check ambient occlusion
			if (PlayerPrefs.GetInt("AmbientOcclusion") == 0)
			{
				ambientocclusiontext.GetComponent<TMP_Text>().text = "off";
			}
			else if (PlayerPrefs.GetInt("AmbientOcclusion") == 1)
			{
				ambientocclusiontext.GetComponent<TMP_Text>().text = "on";
			}

			// check texture quality
			if (PlayerPrefs.GetInt("Textures") == 0)
			{
				QualitySettings.globalTextureMipmapLimit = 2;
				texturelowtextLINE.gameObject.SetActive(true);
				texturemedtextLINE.gameObject.SetActive(false);
				texturehightextLINE.gameObject.SetActive(false);
			}
			else if (PlayerPrefs.GetInt("Textures") == 1)
			{
				QualitySettings.globalTextureMipmapLimit = 1;
				texturelowtextLINE.gameObject.SetActive(false);
				texturemedtextLINE.gameObject.SetActive(true);
				texturehightextLINE.gameObject.SetActive(false);
			}
			else if (PlayerPrefs.GetInt("Textures") == 2)
			{
				QualitySettings.globalTextureMipmapLimit = 0;
				texturelowtextLINE.gameObject.SetActive(false);
				texturemedtextLINE.gameObject.SetActive(false);
				texturehightextLINE.gameObject.SetActive(true);
			}
		}

		public void Update()
		{
			//sliderValue = musicSlider.GetComponent<Slider>().value;
			sliderValueXSensitivity = sensitivityXSlider.GetComponent<Slider>().value;
			sliderValueYSensitivity = sensitivityYSlider.GetComponent<Slider>().value;
			sliderValueSmoothing = mouseSmoothSlider.GetComponent<Slider>().value;
		}

		public void FullScreen()
		{
			Screen.fullScreen = !Screen.fullScreen;

			if (Screen.fullScreen == true)
			{
				fullscreentext.GetComponent<TMP_Text>().text = "on";
			}
			else if (Screen.fullScreen == false)
			{
				fullscreentext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MusicSlider()
		{
			//PlayerPrefs.SetFloat("MusicVolume", sliderValue);
			PlayerPrefs.SetFloat("MusicVolume", musicSlider.GetComponent<Slider>().value);
		}

		public void SensitivityXSlider()
		{
			PlayerPrefs.SetFloat("XSensitivity", sliderValueXSensitivity);
		}

		public void SensitivityYSlider()
		{
			PlayerPrefs.SetFloat("YSensitivity", sliderValueYSensitivity);
		}

		public void SensitivitySmoothing()
		{
			PlayerPrefs.SetFloat("MouseSmoothing", sliderValueSmoothing);
			Debug.Log(PlayerPrefs.GetFloat("MouseSmoothing"));
		}

		// the playerprefs variable that is checked to enable hud while in game
		public void ShowHUD()
		{
			if (PlayerPrefs.GetInt("ShowHUD") == 0)
			{
				PlayerPrefs.SetInt("ShowHUD", 1);
				showhudtext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("ShowHUD") == 1)
			{
				PlayerPrefs.SetInt("ShowHUD", 0);
				showhudtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		// the playerprefs variable that is checked to enable mobile sfx while in game
		public void MobileSFXMute()
		{
			if (PlayerPrefs.GetInt("Mobile_MuteSfx") == 0)
			{
				PlayerPrefs.SetInt("Mobile_MuteSfx", 1);
				mobileSFXtext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("Mobile_MuteSfx") == 1)
			{
				PlayerPrefs.SetInt("Mobile_MuteSfx", 0);
				mobileSFXtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MobileMusicMute()
		{
			if (PlayerPrefs.GetInt("Mobile_MuteMusic") == 0)
			{
				PlayerPrefs.SetInt("Mobile_MuteMusic", 1);
				mobileMusictext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("Mobile_MuteMusic") == 1)
			{
				PlayerPrefs.SetInt("Mobile_MuteMusic", 0);
				mobileMusictext.GetComponent<TMP_Text>().text = "off";
			}
		}

		// show tool tips like: 'How to Play' control pop ups
		public void ToolTips()
		{
			if (PlayerPrefs.GetInt("ToolTips") == 0)
			{
				PlayerPrefs.SetInt("ToolTips", 1);
				tooltipstext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("ToolTips") == 1)
			{
				PlayerPrefs.SetInt("ToolTips", 0);
				tooltipstext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void NormalDifficulty()
		{
			difficultyhardcoretextLINE.gameObject.SetActive(false);
			difficultynormaltextLINE.gameObject.SetActive(true);
			PlayerPrefs.SetInt("NormalDifficulty", 1);
			PlayerPrefs.SetInt("HardCoreDifficulty", 0);
		}

		public void HardcoreDifficulty()
		{
			difficultyhardcoretextLINE.gameObject.SetActive(true);
			difficultynormaltextLINE.gameObject.SetActive(false);
			PlayerPrefs.SetInt("NormalDifficulty", 0);
			PlayerPrefs.SetInt("HardCoreDifficulty", 1);
		}

		public void ShadowsOff()
		{
			PlayerPrefs.SetInt("Shadows", 0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			shadowofftextLINE.gameObject.SetActive(true);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsLow()
		{
			PlayerPrefs.SetInt("Shadows", 1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(true);
			shadowhightextLINE.gameObject.SetActive(false);
		}

		public void ShadowsHigh()
		{
			PlayerPrefs.SetInt("Shadows", 2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			shadowofftextLINE.gameObject.SetActive(false);
			shadowlowtextLINE.gameObject.SetActive(false);
			shadowhightextLINE.gameObject.SetActive(true);
		}

		public void MobileShadowsOff()
		{
			PlayerPrefs.SetInt("MobileShadows", 0);
			QualitySettings.shadowCascades = 0;
			QualitySettings.shadowDistance = 0;
			mobileShadowofftextLINE.gameObject.SetActive(true);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsLow()
		{
			PlayerPrefs.SetInt("MobileShadows", 1);
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 75;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(true);
			mobileShadowhightextLINE.gameObject.SetActive(false);
		}

		public void MobileShadowsHigh()
		{
			PlayerPrefs.SetInt("MobileShadows", 2);
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 500;
			mobileShadowofftextLINE.gameObject.SetActive(false);
			mobileShadowlowtextLINE.gameObject.SetActive(false);
			mobileShadowhightextLINE.gameObject.SetActive(true);
		}

		public void vsync()
		{
			if (QualitySettings.vSyncCount == 0)
			{
				QualitySettings.vSyncCount = 1;
				vsynctext.GetComponent<TMP_Text>().text = "on";
			}
			else if (QualitySettings.vSyncCount == 1)
			{
				QualitySettings.vSyncCount = 0;
				vsynctext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void InvertMouse()
		{
			if (PlayerPrefs.GetInt("Inverted") == 0)
			{
				PlayerPrefs.SetInt("Inverted", 1);
				invertmousetext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("Inverted") == 1)
			{
				PlayerPrefs.SetInt("Inverted", 0);
				invertmousetext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void MotionBlur()
		{
			if (PlayerPrefs.GetInt("MotionBlur") == 0)
			{
				PlayerPrefs.SetInt("MotionBlur", 1);
				motionblurtext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("MotionBlur") == 1)
			{
				PlayerPrefs.SetInt("MotionBlur", 0);
				motionblurtext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void AmbientOcclusion()
		{
			if (PlayerPrefs.GetInt("AmbientOcclusion") == 0)
			{
				PlayerPrefs.SetInt("AmbientOcclusion", 1);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("AmbientOcclusion") == 1)
			{
				PlayerPrefs.SetInt("AmbientOcclusion", 0);
				ambientocclusiontext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void CameraEffects()
		{
			if (PlayerPrefs.GetInt("CameraEffects") == 0)
			{
				PlayerPrefs.SetInt("CameraEffects", 1);
				cameraeffectstext.GetComponent<TMP_Text>().text = "on";
			}
			else if (PlayerPrefs.GetInt("CameraEffects") == 1)
			{
				PlayerPrefs.SetInt("CameraEffects", 0);
				cameraeffectstext.GetComponent<TMP_Text>().text = "off";
			}
		}

		public void TexturesLow()
		{
			PlayerPrefs.SetInt("Textures", 0);
			QualitySettings.globalTextureMipmapLimit = 2;
			texturelowtextLINE.gameObject.SetActive(true);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesMed()
		{
			PlayerPrefs.SetInt("Textures", 1);
			QualitySettings.globalTextureMipmapLimit = 1;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(true);
			texturehightextLINE.gameObject.SetActive(false);
		}

		public void TexturesHigh()
		{
			PlayerPrefs.SetInt("Textures", 2);
			QualitySettings.globalTextureMipmapLimit = 0;
			texturelowtextLINE.gameObject.SetActive(false);
			texturemedtextLINE.gameObject.SetActive(false);
			texturehightextLINE.gameObject.SetActive(true);
		}
	}
}