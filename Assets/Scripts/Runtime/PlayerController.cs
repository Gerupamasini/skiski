using UnityEngine;
using UnityEngine.InputSystem;

namespace Skiski
{
    /// <summary>
    /// 動作確認用のシンプルなプレイヤー操作。
    /// 移動: WASD / 矢印キー / ゲームパッド左スティック
    /// ジャンプ: Space / ゲームパッド South ボタン（A / ×）
    /// 移動方向はカメラの向きが基準になる。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] float moveSpeed = 5f;
        [SerializeField, Min(0f), Tooltip("1 秒あたりの最大回転角（度）")] float rotationSpeed = 720f;
        [SerializeField, Min(0f)] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField, Tooltip("移動方向の基準にするカメラ。未設定なら Main Camera を使う")]
        Transform cameraTransform;

        CharacterController controller;
        InputAction moveAction;
        InputAction jumpAction;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            // 入力アクションをコードで定義（.inputactions アセットに移行してもよい）
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
        }

        void OnDestroy()
        {
            moveAction.Dispose();
            jumpAction.Dispose();
        }

        void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            Vector3 horizontal = ToWorldDirection(input) * moveSpeed;

            if (controller.isGrounded)
            {
                // 接地中は少し下向きの速度を与えて坂道でも地面に張り付かせる
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (jumpAction.WasPressedThisFrame())
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            if (horizontal.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        Vector3 ToWorldDirection(Vector2 input)
        {
            if (cameraTransform == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return forward * input.y + right * input.x;
        }
    }
}
