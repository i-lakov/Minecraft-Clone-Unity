using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    #region Data members
    private Transform cam;
    private World world;

    public bool isGrounded;
    public bool isSprinting;
    public bool isFlying = false;

    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;
    private bool flyRequest;
    private float yRotation = 0;

    public Transform hightlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    private float time = 0f;
    private int pressed = 0;

    public Toolbar toolbar;
    #endregion

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        world.inUI = false;
    }

    private void FixedUpdate()
    {
        if (!world.inUI)
        { 
            CalculateVelocity();
            if (jumpRequest) Jump();
            if (flyRequest) Fly();

            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);

            yRotation += (-mouseVertical * world.settings.mouseSensitivity);
            yRotation = Mathf.Clamp(yRotation, -90, 90);
            cam.transform.eulerAngles = new Vector3(yRotation, cam.transform.eulerAngles.y, cam.transform.eulerAngles.z);

            transform.Translate(velocity, Space.World);

            if (isGrounded)
            {
                gravity = -9.8f;
                isFlying = false;
            }
        }
    }

    private void Update()
    {
        if (isFlying)
        {
            if (Input.GetButtonDown("Jump"))
            {
                pressed++;
                if (pressed == 1) time = Time.time;
            }
            if (pressed > 1 && Time.time - time < 0.3f)
            {
                pressed = 0;
                gravity = -9.8f;
                return;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                verticalMomentum = 0f;
                transform.position += Vector3.up * walkSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                velocity.y = (Vector3.down * walkSpeed).y;
                velocity.y = CheckDownSpeed(velocity.y);

                transform.position += velocity * Time.deltaTime;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            world.inUI = !world.inUI;
        }

        if (world.inUI && Input.GetKeyDown(KeyCode.Escape))
        {
            world.inUI = !world.inUI;
        }

        if (!world.inUI)
        {
            GetPlayerInputs();
            PlaceCursorBlocks();
        }
    }

    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    void Fly()
    {
        gravity = 0;
        isGrounded = false;
        flyRequest = false;
        isFlying = true;
    }

    private void CalculateVelocity()
    {
        // Affect vertical momentum w/ gravity.
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        // Check if sprinting.
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

        // Apply falling & jumping.
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
        {
            velocity.z = 0;
        }

        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        // Falling
        if (velocity.y < 0)
        {
            velocity.y = CheckDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
    }

    private void GetPlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.End))
        {
            Application.Quit();
        }

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
        }
        if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
        }

        // Jumping.
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            time = Time.time;
            jumpRequest = true;
        }
        // Flying.
        if (!isGrounded && Input.GetButtonDown("Jump") && Time.time - time < 0.3f)
        {
            flyRequest = true;
        }

        if (hightlightBlock.gameObject.activeSelf)
        {
            // Destroying block.
            if (Input.GetMouseButtonDown(0))
            {
                world.GetChunkFromVertor3(hightlightBlock.position).EditVoxel(hightlightBlock.position, 0);
            }

            // Placing block.
            if (Input.GetMouseButtonDown(1))
            {
                if (toolbar.slots[toolbar.slotIndex].HasItem)
                {
                    world.GetChunkFromVertor3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                }
            }
        }
    }

    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos))
            {
                hightlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;
                hightlightBlock.gameObject.SetActive(true); // Activate highlight.
                placeBlock.gameObject.SetActive(true); // Activate place highlight.

                return;
            }
            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
            step += checkIncrement;
        }

        hightlightBlock.gameObject.SetActive(false); // Deactivate hightlight.
        placeBlock.gameObject.SetActive(false); // Deactivate place highlight.
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
           world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
           world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
           world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)))
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float CheckUpSpeed(float upSpeed)
    {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)))
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
                return true;

            else return false;
        }
    }

    public bool back
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
                return true;

            else return false;
        }
    }

    public bool left
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;

            else return false;
        }
    }

    public bool right
    {
        get
        {
            if (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;

            else return false;
        }
    }
}
