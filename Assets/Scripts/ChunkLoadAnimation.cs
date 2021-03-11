using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoadAnimation : MonoBehaviour
{
    #region Data members
    float wait;
    float timer;
    float speed = 4f;
    Vector3 target;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        wait = Random.Range(0f, 1.5f);
        target = transform.position;
        transform.position = new Vector3(transform.position.x, -VoxelData.ChunkHeight, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if(timer < wait)
        {
            timer += Time.deltaTime;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);

            if ((target.y - transform.position.y) < 0.05f)
            {
                transform.position = target;
                Destroy(this);
            }
        }
    }
}
