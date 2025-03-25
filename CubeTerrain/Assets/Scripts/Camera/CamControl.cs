using UnityEngine;

public class CamControl : MonoBehaviour
{
    public float rotationSpeed = 10f; // ȸ�� �ӵ�
    public float zoomSpeed = 2f; // Ȯ��/��� �ӵ�
    public float minZoom = 5f; // �ּ� �� �Ÿ�
    public float maxZoom = 500f; // �ִ� �� �Ÿ�

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
        // ���콺 �� Ŭ�� ���¿����� ȸ�� ����
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
        // ���콺 �� ��/�ٿ����� Ȯ��/���
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        float distance = cam.transform.localPosition.z + scroll;

        distance = Mathf.Clamp(distance, -maxZoom, -minZoom);

        cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, distance);
    }
}