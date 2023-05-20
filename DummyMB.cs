using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Logging;
using System.Reflection;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using System.Linq;
using UnityEngine.U2D;
using UnityEngine.UI;
using Hellession;
using System.IO;
using EndListFactions;

public class DummyMB : MonoBehaviour
{
    public void Start()
    {
        Plugin.Log.LogInfo($"Dummy MonoBehaviour reporting for duty!");
        DontDestroyOnLoad(gameObject);
    }

    bool _disabled = false;

    public void Update()
    {
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.E) && !_disabled)
        {
            _disabled = true;
            GlobalServiceLocator.ApplicationService.ShowScene(Scene.Home);
            Destroy(gameObject);
        }
    }
}