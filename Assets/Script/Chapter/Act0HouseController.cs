using UnityEngine;

public class Act0HouseController : MonoBehaviour
{
    [SerializeField] private LockCodePuzzleController lockPuzzle;
    [SerializeField] private PhotoCombinePuzzleController photoPuzzle;
    [SerializeField] private TurntableHotspot recordPuzzle;

    public bool AllSolved =>
        (lockPuzzle == null || lockPuzzle.IsSolved) &&
        (photoPuzzle == null || photoPuzzle.IsSolved) &&
        (recordPuzzle == null || recordPuzzle.IsSolved);
}
