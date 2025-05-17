using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TsunamiManager : MonoBehaviour
{
    public static TsunamiManager Instance;
    public TsunamiDisaster tsunami;

    void Awake()
    {
        Instance = this;
    }

    public void StartTsunami()
    {
        Instantiate(tsunami);
    }
}
