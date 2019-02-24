﻿using System.Collections.Generic;
using UnityEngine;

public class Pedestal : MonoBehaviour, DynamicLightSource
{
    public float activateAnimationSpeed;

    [Header("Connected Triggers")]
    public List<ActivatableObject> connectedTriggers = new List<ActivatableObject>();

    [Header("Emissions")]
    public List<MeshRenderer> emitters;
    public float deactivatedEmitterIntensity = -3.0f;
    public float activatedEmitterIntensity = 3.0f;
    public List<MeshRenderer> renderersInEmission = new List<MeshRenderer>();

    [Header("Lights")]
    public DynamicLightController pointLightController;

    [Header("Testing, should delete later")]
    public float activateDistance = 2.0f;

    public PlayerControllerSimple player;
    private bool activated = false;
    private Color emissionColor;
    private MeshRenderer wireRenderer;

    public int maxLevel;
    public int startLevel;
    int _currLevel;
    private int currLevel
    {
        get
        {
            return _currLevel;
        }
        set
        {
            _currLevel = value;

            if (pointLightController && pointLightController.IsSetup())
                OnLightSourceValueChange(_currLevel);
        }
    }

    /// <summary>
    /// Assign gameobject references and initialize variables
    /// </summary>
    private void Start()
    {
        currLevel = startLevel;
        pointLightController.Setup(this);
        player = FlameKeeper.Get().levelController.GetLevelPlayer();

        if (currLevel > 0)
        {
            activated = true;
            ActivatePedestal();
        }

        if (emitters.Count > 0)
        {
            emissionColor = emitters[0].materials[1].color;
        }
    }

    /// <summary>
    /// Non physics based update
    /// </summary>
    private void Update()
    {
        float playerDistance = Vector3.Distance(this.transform.position, player.transform.position);

        // Check for pedestal activation
        if (Input.GetButtonDown(StringConstants.Input.ActivateButton) && playerDistance < activateDistance)
        {
            if (currLevel < maxLevel && player.RequestLanternUse())
            {
                currLevel++;
                player.RecordCheckpoint();
                ActivatePedestal();
            }
        }

        // Check for pedestal deactivation
        if (Input.GetButtonDown(StringConstants.Input.DeactivateButton) && playerDistance < activateDistance)
        {
            if (activated && player.RequestLanternAddition())
            {
                currLevel--;
                player.RecordCheckpoint();
                if (currLevel <= 0)
                {
                    DeactivatePedestal();
                    currLevel = 0;
                }
                else
                {
                    ActivatePedestal();
                }
            }
        }

        int i = 0;
        foreach (MeshRenderer emitter in emitters)
        {
            // TODO: Should lerp these values to get a nice animation
            if (i < currLevel)
            {
                emitter.materials[1].SetVector("_Color", emissionColor);
                emitter.materials[1].SetVector("_EmissionColor", emissionColor * activatedEmitterIntensity * activatedEmitterIntensity * Mathf.Sign(activatedEmitterIntensity));
            }
            else
            {
                emitter.materials[1].SetVector("_Color", emitter.materials[0].color);
                emitter.materials[1].SetVector("_EmissionColor", emitter.materials[0].color * deactivatedEmitterIntensity * deactivatedEmitterIntensity * Mathf.Sign(deactivatedEmitterIntensity));
            }
            i++;
        }

        // If we are updating an emission intensity, we have to tell all materials that it touches
        // to re compute their global illumination. There is probably a better method to do this but
        // for now I think this is ok.
        foreach (MeshRenderer rend in renderersInEmission)
        {
            rend.UpdateGIMaterials();
        }
    }

    /// <summary>
    /// Notifies light controller of new value
    /// </summary>
    public void OnLightSourceValueChange(int newValue)
    {
        pointLightController.CalculateLightTargets(newValue);
    }

    /// <summary>
    /// Returns the number of lantern uses the player currently has
    /// </summary>
    /// <returns></returns>
    public int GetLightSourceValue()
    {
        return GetCurrLevel();
    }

    /// <summary>
    /// Checks if the pedestal has any amount of charge in it
    /// </summary>
    public bool IsActive()
    {
        return activated;
    }

    public void ActivatePedestal()
    {
        activated = true;
        foreach (ActivatableObject obj in connectedTriggers)
        {
            obj.OnPedestalActivate(this);
        }
    }

    public void DeactivatePedestal()
    {
        foreach (ActivatableObject obj in connectedTriggers)
        {
            obj.OnPedestalDeactivate(this);
        }

        activated = false;
    }

    /// <summary>
    /// Gets the current number of charges in the pedestal
    /// </summary>
    public int GetCurrLevel()
    {
        return currLevel;
    }
}