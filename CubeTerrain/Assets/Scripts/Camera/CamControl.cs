using UnityEngine;

public class CamControl : MonoBehaviour
{
    public float rotationSpeed = 10f; // 회전 속도
    public float zoomSpeed = 2f; // 확대/축소 속도
    public float minZoom = 5f; // 최소 줌 거리
    public float maxZoom = 500f; // 최대 줌 거리

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
    }

    void HandleRotation()
    {
        // 마우스 휠 클릭 상태에서만 회전 가능
        if (Input.GetMouseButton(2))
        {
            float horizontal = Input.GetAxis("Mouse X") * rotationSpeed;
            float vertical = -Input.GetAxis("Mouse Y") * rotationSpeed;

            transform.Rotate(Vector3.up, horizontal, Space.World);
            transform.Rotate(Vector3.right, vertical, Space.Self);
        }
    }

    void HandleZoom()
    {
        // 마우스 휠 업/다운으로 확대/축소
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        float distance = cam.transform.localPosition.z + scroll;

        distance = Mathf.Clamp(distance, -maxZoom, -minZoom);

        cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, distance);
    }
}