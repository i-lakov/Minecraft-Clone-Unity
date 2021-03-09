using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropHandler : MonoBehaviour
{
    [SerializeField]
    private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField]
    private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;

    [SerializeField]
    private EventSystem m_EventSystem = null;

    World world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if(!world.inUI)
        {
            return;
        }

        cursorSlot.transform.position = Input.mousePosition;

        if(Input.GetMouseButtonDown(0))
        {
            HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
        {
            return;
        }

        if(!cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            return;
        }

        if(clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        // "Take" items from slot.
        if(!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        // "Place" items in empty slot.
        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            // Swap held item and slot item.
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack temp = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(temp);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }
            else // If items are the same, stack them up.
            {
                ItemStack temp = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                temp.amount += oldSlot.amount;
                if(temp.amount > 64)
                {
                    int temporaryInt = temp.amount - 64;
                    temp.amount = 64;
                    oldSlot.amount = temporaryInt;
                }

                clickedSlot.itemSlot.InsertStack(temp);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }

            // TODO if items are the same, stack them together
        }
    }

    private UIItemSlot CheckForSlot()
    {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach(RaycastResult result in results)
        {
            if (result.gameObject.tag == "UIItemSlot")
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
