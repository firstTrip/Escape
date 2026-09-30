using UnityEngine;

public class PhoneHotspot : InteractableHotspot
{
    [SerializeField] private PrologueSequence prologue;

    protected override void OnInteract()
    {
        prologue?.ReplayVoicemail();
    }
}
