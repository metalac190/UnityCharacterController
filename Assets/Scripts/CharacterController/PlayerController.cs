using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Camera _playerCamera = null;
    public Camera PlayerCamera { get { return _playerCamera; } }

    // can't see interfaces in inspector. search on Start
    [SerializeField] Pawn _startingPawn = null;

    public Pawn ActivePawn { get; private set; }
    public Pawn PreviousPawn { get; private set; }

    PlayerInput _playerInput;

    private void Awake()
    {
        // add references here
        _playerInput = GetComponent<PlayerInput>();
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
        if(_startingPawn != null)
        {
            Control(_startingPawn);
        }
    }

    public void Control(Pawn pawn)
    {
        Debug.Log("Controlled the pawn!");
        // first release the previous player
        if (ActivePawn != null)
        {
            Release();
        }

        pawn.Control(this, _playerInput, _playerCamera);
        ActivePawn = pawn;
    }

    public void Release()
    {
        Debug.Log("Released the Pawn!");
        PreviousPawn = ActivePawn;
        ActivePawn.Release();
        ActivePawn = null;
    }

    void OnEscapeKey()
    {
        Release();
    }
}
