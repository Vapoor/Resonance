using MidiJack;
using UnityEngine;

public class AkaiImputController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < 128; i++)
        {
            if (MidiMaster.GetKeyDown(i))
            {
                Debug.Log("NOTE DOWN : " + i);
            }

            if (MidiMaster.GetKeyUp(i))
            {
                Debug.Log("NOTE UP : " + i);
            }
        }
    }
}
