using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;


public class PlayerController : NetworkBehaviour
{
  
    public int id_player;

    private Vector3 movementVector;
    private Vector3 lookDirection;
    public GameObject theBall;

    private Transform transformItemInHand;
    public GameObject itemInHand;

    private string horizontalCtrl = "Horizontal_P1";
    private string verticalCtrl = "Vertical_P1";
    private string takeCtrl = "Take_P1";
    private string interractionCtrl = "Action_P1";
    private string dashCtrl = "Dash_P1";

    private CharacterController characterController;
    // public List<GameObject> gameObjectsInterractable; //Les pouvoirs/objets à utiliser

    public float movementSpeed = 10;
    public float boostFromDash = 200;
    public float boostTime = 0.001f;
    public float currentCooldown = 0;
    public float dashCooldownTime = 1;
    private float initialSpeed;
    private bool isDashing = false;
    
    private NetworkManager networkManager;



    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        initialSpeed = movementSpeed;
        transformItemInHand = this.gameObject.transform.GetChild(0);
    }
    
    // Update is called once per frame    
    void Update()
    {
        //CoolDown du dash
        if (currentCooldown < dashCooldownTime)
        {
            currentCooldown += Time.deltaTime;
            if (currentCooldown > dashCooldownTime)
                currentCooldown = dashCooldownTime;
        }

        //Mouvement du perso
        float horizontalInput = Input.GetAxis(horizontalCtrl);
        float verticalInput = Input.GetAxis(verticalCtrl);

        movementVector.x = horizontalInput; // * movementSpeed;
        movementVector.y = 0;
        movementVector.z = verticalInput; //* movementSpeed;
        
        if (movementVector.magnitude > 1)
            movementVector.Normalize();

        characterController.Move(movementVector * Time.deltaTime * movementSpeed);


        //rotation
        if (characterController.velocity.x != 0)
            lookDirection.x = characterController.velocity.x;

        if (characterController.velocity.z != 0)
            lookDirection.z = characterController.velocity.z;

        if (lookDirection.x != 0 || lookDirection.z != 0)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 0.3f);

        //Action
  

        //use power
        
    }


    private IEnumerator Dash(float a_Delay)
    {
        yield return new WaitForSeconds(a_Delay);
        movementSpeed = initialSpeed;

        if (movementSpeed < initialSpeed)
        {
            movementSpeed = initialSpeed;
            isDashing = false;
        }
    }




}

