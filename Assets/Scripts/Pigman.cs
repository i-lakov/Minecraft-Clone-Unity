using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pigman : MonoBehaviour
{
    #region Data members
    CharacterController characterController;

    private Vector3 velocity = Vector3.zero;
    private Vector3 initialPosition = Vector3.zero;
    private Random rand = new Random();
    private int direction;

    [SerializeField] 
    float movementSpeed;
    #endregion

    public Pigman(Vector3 pos)
    {
        transform.position = pos;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        initialPosition = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(transform.position.x > 820)
        {
            velocity.x = -movementSpeed;
        }
        if(transform.position.x < 780)
        {
            velocity.x = movementSpeed;
        }

        characterController.Move(velocity * Time.fixedDeltaTime);
    }
}
