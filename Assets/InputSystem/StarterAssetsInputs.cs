using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		[Tooltip("Input di chuyển (X ngang, Y dọc)")]
		public Vector2 move;
		[Tooltip("Input nhìn camera (X yaw, Y pitch)")]
		public Vector2 look;
		[Tooltip("Trạng thái nút nhảy")]
		public bool jump;
		[Tooltip("Trạng thái nút chạy nhanh")]
		public bool sprint;

		[Header("Movement Settings")]
		[Tooltip("Bật để dùng input analog cho di chuyển")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		[Tooltip("Giữ khóa con trỏ chuột vào màn hình game")]
		public bool cursorLocked = true;
		[Tooltip("Cho phép input chuột điều khiển hướng nhìn")]
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if (cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		}

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}

}