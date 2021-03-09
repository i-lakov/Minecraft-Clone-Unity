using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{
    public GameObject slotPrefab;
    World world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        
        // Populate the grid with blocks
        for(int id = 1; id < world.blocktypes.Length; id++)
        {
            GameObject newSlot = Instantiate(slotPrefab, transform);

            ItemStack stack = new ItemStack((byte)id, 64);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack, true);
        }
    }
}
