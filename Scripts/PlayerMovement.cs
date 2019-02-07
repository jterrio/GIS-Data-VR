using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour{

    public int speed = 5;
    private Rigidbody rb;

    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

    void Start() {
        rb = GetComponent<Rigidbody>();
        Vector3 rot = Camera.main.transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
    }

    // Update is called once per frame
    void Update(){
        Vector3 movementVector = Vector3.zero;

        if (Input.GetKey(KeyCode.A))
            movementVector += new Vector3(-1, 0, 0);
        if (Input.GetKey(KeyCode.D))
            movementVector += new Vector3(1, 0, 0);
        if (Input.GetKey(KeyCode.W))
            movementVector += new Vector3(0, 0, 1);
        if (Input.GetKey(KeyCode.S))
            movementVector += new Vector3(0, 0, -1);
        if (Input.GetKey(KeyCode.Space))
            movementVector += new Vector3(0, 1, 0);
        if (Input.GetKey(KeyCode.LeftControl))
            movementVector += new Vector3(0, -1, 0);


        transform.position += movementVector * speed * Time.deltaTime;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        Camera.main.transform.rotation = localRotation;
        transform.rotation = localRotation;
    }
}
