using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> MakeTree(Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> q = new Queue<VoxelMod>();

        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f));

        // No tree should be smaller than the minimum.
        if(height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // LEAVES
        // Main body of leaves.
        for(int x = -2; x < 3; x++)
        {
            for (int y = 1; y < 3; y++)
            {
                for (int z = -2; z < 3; z++)
                {
                    q.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y + height, position.z + z), 11));
                }
            }
        }

        // Top-most and bottom-most parts of the body of the leaves.
        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                if ((x == -2 && z == -2) || (x == 2 && z == 2) || (x == -2 && z == 2) || (x == 2 && z == -2)) { } // Don't create leaves in the BOTTOM corners, as it's more realistic.
                else q.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height, position.z + z), 11));
            }
        }
        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                if ((x == -2 && z == -2) || (x == 2 && z == 2) || (x == -2 && z == 2) || (x == 2 && z == -2)) { } // Don't create leaves in the TOP corners, as it's more realistic.
                else q.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + 3, position.z + z), 11));
            }
        }

        // A smaller layer overtop all others.
        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                q.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + 4 + height, position.z + z), 11));
            }
        }

        // LOGS
        for (int i = 1; i < height + 3; i++) // + 3 to make the trunk go inside the leaves section
        {
            q.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
        }

        return q;
    }
}
