﻿using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitSpawner : NetworkBehaviour,IPointerClickHandler
{
    [SerializeField] private Health health = null;
    [SerializeField] private Unit unitPrefab = null;

    [SerializeField] private Transform unitSpawnPoint = null;
    [SerializeField] private TMP_Text remainingUnitsText = null;
    [SerializeField] private Image unitProgressImage = null;

    [SerializeField] private int maxUnitQueue = 5;
    [SerializeField] private float spawnMoveRange = 7f;
    [SerializeField] private float unitSpawnDuration = 7f;

    
    [SyncVar (hook = nameof(ClientHandleQueuedUnitsUpdated))]
    private int queuedUnits;
    [SyncVar]
    private float unitTimer;

    private float progressImageVelocity;
    private void Update()
    {
        if(isServer)
        {
            ProduceUnits();
        }

        if(isClient)
        {
            UpdateTimerDisplay();
        }



    }

    #region Server


    public override void OnStartServer()
    {
        health.ServerOnDie += ServerHandleDie; //subs
        
    }


    public override void OnStopServer()
    {
        health.ServerOnDie -= ServerHandleDie;  //unsubs
    }


    [Server]
    private void ProduceUnits()
    {
        if(queuedUnits == 0) 
        { return; }

        unitTimer += Time.deltaTime;

        if(unitTimer < unitSpawnDuration)
        { return; }

        GameObject unitIstance = Instantiate(
           unitPrefab.gameObject ,
           unitSpawnPoint.position,
           unitSpawnPoint.rotation);

        NetworkServer.Spawn(unitIstance, connectionToClient); //Player unit'e hakim olur

        Vector3 spawnOffset = Random.insideUnitSphere * spawnMoveRange;
        spawnOffset.y = unitSpawnPoint.position.y;

        UnitMovement unitMovement = unitIstance.GetComponent<UnitMovement>();
        unitMovement.ServerMove(unitSpawnPoint.position - spawnOffset);

        queuedUnits--;
        unitTimer = 0f;
    }



    [Server]
    private void ServerHandleDie()
    {
       NetworkServer.Destroy(gameObject);

    }


    [Command]
    private void CmdSpawnUnit()
    {
       if(queuedUnits == maxUnitQueue) 
        { return; }

        RTSPlayer player = connectionToClient.identity.GetComponent<RTSPlayer>();

        if(player.GetGold() < unitPrefab.GetGoldCost()) 
        { return; }

        queuedUnits++;

        player.SetGold(player.GetGold() - unitPrefab.GetGoldCost());


    }


    #endregion Server
   

    #region Client

    private void UpdateTimerDisplay()
    {
        float newProgress = unitTimer / unitSpawnDuration;

        if(newProgress < unitProgressImage.fillAmount)
        {
            unitProgressImage.fillAmount = newProgress;
        }

        else
        {
            unitProgressImage.fillAmount = Mathf.SmoothDamp(unitProgressImage.fillAmount, newProgress, ref progressImageVelocity, 0.1f);
        }

    }


     public void OnPointerClick(PointerEventData eventData)
     {
         if(eventData.button!= PointerEventData.InputButton.Left)
        { return; }

         if(!hasAuthority)
        { return; }

        CmdSpawnUnit();

     }

    private void ClientHandleQueuedUnitsUpdated(int oldUnits, int newUnits)
    {
        remainingUnitsText.text = newUnits.ToString();
    }


    #endregion Client
}
