using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Camera _playerCamera = null;
    public Camera PlayerCamera { get { return _playerCamera; } }

    // can't see interfaces in inspector. search on Start
    [SerializeField] GameObject _startingPawn = null;
    IPawn _potentialStartingPawn;

    public IPawn ActivePawn { get; private set; }
    public IPawn PreviousPawn { get; private set; }

    PlayerInput _playerInput;

    private void Awake()
    {
        // add references here
        _playerInput = GetComponent<PlayerInput>();

        // see if we have a valid starting pawn by searching gameObject
        // for our interface
        _potentialStartingPawn = _startingPawn?.GetComponent<IPawn>();
        if(_potentialStartingPawn == null)
        {
            Debug.Log("Starting Pawn");
        }
        else
        {
            Debug.Log("Found Starting Pawn!");
        }
    }

    private void OnEnable()
    {
        _playerInput.EscapeKey += OnEscapeKey;
    }

    private void OnDisable()
    {
        _playerInput.EscapeKey -= OnEscapeKey;
    }

    private void Start()
    {
        // if we have a valid starting pawn, control it
        if(_potentialStartingPawn != null)
        {
            Control(_potentialStartingPawn);
        }
    }

    public void Control(IPawn pawn)
    {
        Debug.Log("Controlled the pawn!");
        // first release the previous player
        if (ActivePawn != null)
        {
            Release();
        }

        pawn.OnControlled(this, _playerInput, _playerCamera);
        ActivePawn = pawn;
    }

    public void Release()
    {
        Debug.Log("Released the Pawn!");
        PreviousPawn = ActivePawn;
        ActivePawn.OnReleased(this, _playerInput, _playerCamera);
        ActivePawn = null;
    }

    void OnEscapeKey()
    {
        Release();
    }
}
