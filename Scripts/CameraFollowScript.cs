using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour{

    [Header("Object To Follow")]
    public GameObject player;


    // Update is called once per frame
    void Update(){
        gameObject.transform.position = player.transform.position;
    }

}
