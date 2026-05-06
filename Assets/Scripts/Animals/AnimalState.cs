// Assets/Scripts/Animals/AnimalState.cs
namespace DeenCraft.Animals
{
    public enum AnimalState
    {
        Idle   = 0,
        Wander = 1,
        Flee   = 2,
        Follow = 3,
        Graze  = 4,  // passive idle variant for Cow and Sheep
    }
}
