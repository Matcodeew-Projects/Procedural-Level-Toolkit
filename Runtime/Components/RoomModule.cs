using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomModule
    : MonoBehaviour
{
    // =========================================================
    // Module Definition
    // =========================================================

    [SerializeField]
    private RoomModuleDefinition definition;


    // =========================================================
    // Legacy / Generated Room Data
    // =========================================================
    //
    // Ces deux références sont conservées car le RoomGenerator
    // construit actuellement le prefab directement depuis :
    //
    // RoomDefinition
    // +
    // RoomGenerationSettings
    //
    // Le Level Editor utilise ensuite RoomModuleDefinition.
    //
    // Les deux workflows peuvent donc coexister.
    // =========================================================

    [SerializeField]
    private RoomDefinition roomDefinition;

    [SerializeField]
    private RoomGenerationSettings generationSettings;


    // =========================================================
    // Properties
    // =========================================================

    public RoomModuleDefinition Definition =>
        definition;


    public RoomDefinition Room
    {
        get
        {
            if (definition != null &&
                definition.Room != null)
            {
                return definition.Room;
            }


            return roomDefinition;
        }
    }


    public RoomGenerationSettings GenerationSettings =>
        generationSettings;


    public bool HasDefinition =>
        definition != null;


    public bool HasRoom =>
        Room != null;


    // =========================================================
    // Configure From Room Generator
    // =========================================================
    //
    // Utilisé par RoomGenerator :
    //
    // roomModule.Configure(
    //     room,
    //     settings
    // );
    //
    // =========================================================

    public void Configure(
        RoomDefinition room,
        RoomGenerationSettings settings
    )
    {
        roomDefinition =
            room;


        generationSettings =
            settings;


        /*
         * Si le prefab possédait déjà une définition de module
         * correspondant à cette Room, on peut la conserver.
         *
         * Sinon on la retire pour éviter qu'un RoomModule
         * référence une définition appartenant à une autre Room.
         */

        if (definition != null &&
            definition.Room != room)
        {
            definition =
                null;
        }
    }


    // =========================================================
    // Configure From Level Module
    // =========================================================
    //
    // Utilisé par RoomModuleEditorUtility / LevelBuilder.
    //
    // =========================================================

    public void Configure(
        RoomModuleDefinition value
    )
    {
        definition =
            value;


        if (definition != null)
        {
            roomDefinition =
                definition.Room;
        }
    }


    // =========================================================
    // Full Configure
    // =========================================================
    //
    // Utile plus tard si on veut construire / mettre à jour
    // complètement le marker en une seule opération.
    //
    // =========================================================

    public void Configure(
        RoomModuleDefinition moduleDefinition,
        RoomDefinition room,
        RoomGenerationSettings settings
    )
    {
        definition =
            moduleDefinition;


        roomDefinition =
            room;


        generationSettings =
            settings;


        if (definition != null &&
            definition.Room != null)
        {
            roomDefinition =
                definition.Room;
        }
    }


    // =========================================================
    // Individual Setters
    // =========================================================

    public void SetDefinition(
        RoomModuleDefinition value
    )
    {
        definition =
            value;


        if (definition != null &&
            definition.Room != null)
        {
            roomDefinition =
                definition.Room;
        }
    }


    public void SetRoom(
        RoomDefinition value
    )
    {
        roomDefinition =
            value;


        if (definition != null &&
            definition.Room != value)
        {
            definition =
                null;
        }
    }


    public void SetGenerationSettings(
        RoomGenerationSettings value
    )
    {
        generationSettings =
            value;
    }
}