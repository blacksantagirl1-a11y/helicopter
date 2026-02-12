using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public GameObject player;
    public Transform holdPos;
    public float throwForce = 500f; //lực mà vật thể được ném
    public float pickUpRange = 5f; //khoảng cách tối đa người chơi có thể nhặt vật thể
    public PlayerUI playerUI; //tham chiếu đến UI của người chơi
    public string doorInteractionMessage = "mở/đóng cửa"; //thông báo mặc định cho cửa
    public string pickUpMessage = "nhặt"; //thông báo mặc định cho vật thể có thể nhặt
    
    private GameObject heldObj; //vật thể mà chúng ta đang nhặt
    private Rigidbody heldObjRb; //rigidbody của vật thể chúng ta nhặt
    private int LayerNumber; //chỉ số layer
    private Interactable currentInteractable; //vật thể có thể tương tác hiện tại đang nhìn vào
    private GameObject currentPickUpObject; //vật thể có thể nhặt hiện tại đang nhìn vào

    void Start()
    {
        LayerNumber = LayerMask.NameToLayer("holdLayer"); //nếu holdLayer của bạn có tên khác, hãy đảm bảo thay đổi tên này ""
        
        // Tự động tìm PlayerUI nếu chưa được gán
        if (playerUI == null)
        {
            playerUI = FindObjectOfType<PlayerUI>();
        }
    }
    void Update()
    {
        // Kiểm tra liên tục vật thể đang nhìn vào để hiển thị UI
        CheckForInteractables();
        
        if (Input.GetKeyDown(KeyCode.E)) //thay đổi E thành phím bất kỳ bạn muốn nhấn để nhặt
        {
            if (heldObj == null) //nếu hiện tại không đang cầm gì
            {
                
                //thực hiện raycast để kiểm tra xem người chơi có đang nhìn vào vật thể trong phạm vi nhặt không
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickUpRange))
                {
                    // 1) mo cua
                    Interactable interactable = hit.transform.GetComponentInParent<Interactable>();
                    if (interactable != null)
                    {
                        interactable.BaseInteract();
                        HideUI(); //ẩn UI sau khi tương tác
                        return;
                    }

                    // 2) Vật thể có thể nhặt
                    if (hit.transform.gameObject.tag == "canPickUp")
                    {
                        //truyền vật thể bị va chạm vào hàm PickUpObject
                        PickUpObject(hit.transform.gameObject);
                        HideUI(); //ẩn UI sau khi nhặt
                    }
                }
            }
            else
            {
                StopClipping(); //prevents object from clipping through walls
                DropObject();
                HideUI(); //ẩn UI sau khi thả
            }
        }
        if (heldObj != null) //nếu người chơi đang cầm vật thể
        {
            MoveObject(); //giữ vị trí vật thể tại holdPos
            if (Input.GetKeyDown(KeyCode.Mouse0)) //Mouse0 (click trái) được dùng để ném, thay đổi nếu bạn muốn dùng nút khác
            {
                StopClipping();
                ThrowObject();
                HideUI(); //ẩn UI sau khi ném
            }

        }
    }
    
    void CheckForInteractables()
    {
        // Chỉ kiểm tra khi không đang cầm vật thể
        if (heldObj != null)
        {
            HideUI();
            return;
        }
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, pickUpRange))
        {
            // Kiểm tra Interactables (doors, etc.)
            Interactable interactable = hit.transform.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                // Nếu đây là vật thể mới, cập nhật UI
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    currentPickUpObject = null;
                    
                    // Hiển thị thông báo từ interactable hoặc thông báo mặc định
                    string message = !string.IsNullOrEmpty(interactable.pickUpMessage) 
                        ? interactable.pickUpMessage 
                        : doorInteractionMessage;
                    ShowUI(message);
                }
                return;
            }
            
            // Kiểm tra vật thể có thể nhặt
            if (hit.transform.gameObject.tag == "canPickUp")
            {
                // Nếu đây là vật thể mới, cập nhật UI
                if (currentPickUpObject != hit.transform.gameObject)
                {
                    currentPickUpObject = hit.transform.gameObject;
                    currentInteractable = null;
                    ShowUI(pickUpMessage);
                }
                return;
            }
        }
        
        // Không nhìn vào gì, ẩn UI
        if (currentInteractable != null || currentPickUpObject != null)
        {
            HideUI();
            currentInteractable = null;
            currentPickUpObject = null;
        }
    }
    
    void ShowUI(string message)
    {
        if (playerUI != null)
        {
            playerUI.UpdateText(message);
        }
    }
    
    void HideUI()
    {
        if (playerUI != null)
        {
            playerUI.UpdateText("");
        }
    }

    void PickUpObject(GameObject pickUpObj)
    {
        if (pickUpObj.GetComponent<Rigidbody>()) //đảm bảo vật thể có RigidBody
        {
            heldObj = pickUpObj; //gán heldObj cho vật thể bị raycast trúng (không còn == null)
            heldObjRb = pickUpObj.GetComponent<Rigidbody>(); //gán Rigidbody
            heldObjRb.isKinematic = true;
            heldObjRb.transform.parent = holdPos.transform; //gán vật thể làm con của holdposition
            heldObj.layer = LayerNumber; //thay đổi layer của vật thể thành holdLayer
            //đảm bảo vật thể không va chạm với người chơi, có thể gây lỗi kỳ lạ
            Collider heldCollider = heldObj.GetComponent<Collider>();
            Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
            if (heldCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(heldCollider, playerCollider, true);
            }
        }
    }
    void DropObject()
    {
        if (heldObj == null) return;
        
        //re-enable collision with player
        Collider heldCollider = heldObj.GetComponent<Collider>();
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        if (heldCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(heldCollider, playerCollider, false);
        }
        heldObj.layer = 0; //vật thể được gán lại về layer mặc định
        if (heldObjRb != null)
        {
            heldObjRb.isKinematic = false;
        }
        heldObj.transform.parent = null; //bỏ gán vật thể làm con
        heldObj = null; //hủy định nghĩa game object
    }
    void MoveObject()
    {
        //keep object position the same as the holdPosition position
        if (heldObj != null && holdPos != null)
        {
            heldObj.transform.position = holdPos.transform.position;
        }
    }
    void ThrowObject()
    {
        if (heldObj == null) return;
        
        //giống như hàm drop, nhưng thêm lực vào vật thể trước khi hủy định nghĩa nó
        Collider heldCollider = heldObj.GetComponent<Collider>();
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        if (heldCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(heldCollider, playerCollider, false);
        }
        heldObj.layer = 0;
        if (heldObjRb != null)
        {
            heldObjRb.isKinematic = false;
            heldObjRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; //ngăn vật thể xuyên qua tường khi ném
            heldObjRb.AddForce(transform.forward * throwForce);
        }
        heldObj.transform.parent = null;
        heldObj = null;
    }
    void StopClipping() //function only called when dropping/throwing
    {
        if (heldObj == null) return;
        
        var clipRange = Vector3.Distance(heldObj.transform.position, transform.position); //khoảng cách từ holdPos đến camera
        //phải sử dụng RaycastAll vì vật thể chặn raycast ở giữa màn hình
        //RaycastAll trả về mảng tất cả các collider bị va chạm trong cliprange
        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.TransformDirection(Vector3.forward), clipRange);
        //nếu độ dài mảng lớn hơn 1, nghĩa là nó đã va chạm nhiều hơn chỉ vật thể chúng ta đang mang
        if (hits.Length > 1)
        {
            //thay đổi vị trí vật thể về vị trí camera
            heldObj.transform.position = transform.position + new Vector3(0f, -0.5f, 0f); //lệch xuống một chút để ngăn vật thể rơi phía trên người chơi
            //nếu người chơi của bạn nhỏ, thay đổi -0.5f thành số nhỏ hơn (về độ lớn) ví dụ: -0.1f
        }
    }
}