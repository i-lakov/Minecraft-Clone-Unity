using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// "front-end" class
public class UIItemSlot : MonoBehaviour
{
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public Text slotAmount;

    World world;

    private void Awake()
    {
        world = GameObject.Find("World").GetComponent<World>();
    }
    
    public bool HasItem
    {
        get
        {
            if (itemSlot == null)
            {
                return false;
            }
            else return itemSlot.HasItem;
        }
    }

    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void Unlink()
    {
        itemSlot.UnlinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {
            slotIcon.sprite = world.blocktypes[itemSlot.stack.id].icon;
            if(itemSlot.stack.amount > 1)
            {
                if (itemSlot.isCreative)
                { 
                    slotAmount.text = ""; 
                }
                else slotAmount.text = itemSlot.stack.amount.ToString();
            }
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else Clear();
    }

    public void Clear()
    {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy()
    {
        if(isLinked)
        {
            itemSlot.UnlinkUISlot();
        }
    }
}

// "back-end" class
public class ItemSlot
{
    #region Data members
    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative;
    #endregion

    #region Constructors
    public ItemSlot(UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _stack, bool _isCreative)
    {
        stack = _stack;
        uiItemSlot = _uiItemSlot;
        isCreative = _isCreative;
        uiItemSlot.Link(this);
    }
    #endregion

    public void LinkUISlot(UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void UnlinkUISlot()
    {
        uiItemSlot = null;
    }

    public void EmptySlot()
    {
        stack = null;
        if(uiItemSlot != null)
        {
            uiItemSlot.UpdateSlot();
        }
    }

    public int Take(int _amount)
    {
        if(_amount > stack.amount)
        {
            int temp = stack.amount;
            EmptySlot();
            return temp;
        }
        else if(_amount < stack.amount)
        {
            stack.amount -= _amount;
            uiItemSlot.UpdateSlot();
            return _amount;
        }
        else
        {
            EmptySlot();
            return _amount;
        }
    }

    public ItemStack TakeAll()
    {
        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertStack(ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get
        {
            if (stack != null)
            {
                return true;
            }
            else return false;
        }
    }
}
