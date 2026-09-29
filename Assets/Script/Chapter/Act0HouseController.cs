using UnityEngine;

public class Act0HouseController : MonoBehaviour
{
    [SerializeField] private LockCodePuzzleController lockPuzzle;
    [SerializeField] private PhotoCombinePuzzleController photoPuzzle;
    [SerializeField] private TurntableHotspot recordPuzzle;

    public bool AllSolved => IsSolved(lockPuzzle) && IsSolved(photoPuzzle) && IsSolved(recordPuzzle);

    private static bool IsSolved(MonoBehaviour puzzleComponent)
    {
        return puzzleComponent == null || puzzleComponent is IPuzzle { IsSolved: true };
    }
}
