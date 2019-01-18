using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour{

    public int speed = 5;
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
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
    }
}
