using UnityEngine;

public sealed class RoomModule
    : MonoBehaviour
{
    [SerializeField]
    private RoomDefinition roomDefinition;

    [SerializeField]
    private RoomGenerationSettings generationSettings;


    public RoomDefinition RoomDefinition =>
        roomDefinition;


    public RoomGenerationSettings GenerationSettings =>
        generationSettings;


    public void Configure(
        RoomDefinition room,
        RoomGenerationSettings settings)
    {
        roomDefinition =
            room;

        generationSettings =
            settings;
    }
}