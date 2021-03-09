using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public Player player;
    public int slotIndex = 0;

    private void Start()
    {
        byte index = 1;
        foreach(UIItemSlot s in slots)
        {
            ItemStack stack = new ItemStack(index, Random.Range(2, 65));
            ItemSlot slot = new ItemSlot(s, stack);
            index++;
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if(scroll != 0)
        {
            if (scroll > 0)
            {
                slotIndex--;
            }
            else slotIndex++;

            if(slotIndex > slots.Length - 1)
            {
                slotIndex = 0;
            }
            if (slotIndex < 0)
            {
                slotIndex = slots.Length - 1;
            }

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
        else
        {
            if (Input.GetButtonDown("Btn1"))
            {
                slotIndex = 0;
            }
            else if (Input.GetButtonDown("Btn2"))
            {
                slotIndex = 1;
            }
            else if (Input.GetButtonDown("Btn3"))
            {
                slotIndex = 2;
            }
            else if (Input.GetButtonDown("Btn4"))
            {
                slotIndex = 3;
            }
            else if (Input.GetButtonDown("Btn5"))
            {
                slotIndex = 4;
            }
            else if (Input.GetButtonDown("Btn6"))
            {
                slotIndex = 5;
            }
            else if (Input.GetButtonDown("Btn7"))
            {
                slotIndex = 6;
            }
            else if (Input.GetButtonDown("Btn8"))
            {
                slotIndex = 7;
            }
            else if (Input.GetButtonDown("Btn9"))
            {
                slotIndex = 8;
            }

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
        
    }
}
