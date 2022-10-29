#nullable disable

namespace AoE4WorldReplaysParser.Models;

public enum EntityCategory : short
{
    Building = 19,
    Unit = 46,
    Upgrade = 52
}

public class DataSTLSCreatedEntity : DataModelBase, IDeserializable
{
    public float timestamp;
    public int playerId;
    public int entityId;
    [ExpectedValue(1, "Notice: Was always 1")]
    public byte unknown2;
    public string entityType;
    [ExpectedValue(0, "Notice: Was always 0")]
    public short unknown4;
    public EntityCategory entityCategory;
    public ResourceDict unknownItems1;
    public ResourceDict unknownItems2;
    public float x;
    public float y;

    public void Deserialize(RelicBlobReader reader)
    {
        timestamp = reader.ReadSingle();
        playerId = reader.ReadInt32();
        entityId = reader.ReadInt32();
        unknown2 = reader.ReadByte();
        entityType = reader.ReadPrefixedString();
        unknown4 = reader.ReadInt16();
        entityCategory = (EntityCategory)reader.ReadInt16();
        unknownItems1 = reader.ReadStruct<ResourceDict>();
        unknownItems2 = reader.ReadStruct<ResourceDict>();
        x = reader.ReadSingle();
        y = reader.ReadSingle();
    }

    public override string ToString()
    {
        return $"{timestamp}: {entityId}:{entityType} [{x}, {y}]";
    }
}

public class DataSTLSLostEntity : DataModelBase, IDeserializable
{
    public float timestamp;

    public int targetPlayerId;
    public int targetEntityId;
    [ExpectedValue(1, "Warning: Was always 1. If it's 0 then probably the rest of the structure changes", true)]
    public byte hasTarget;
    public string targetUnitType;
    [ExpectedValue(0, "Notice: Was always 0")]
    public short unknown3;
    public short unknown4;

    public int attackerPlayerId;
    public int attackerEntityId;
    public byte hasAttacker;
    public string attackerUnitType;
    [ExpectedValue(0, "Notice: Was always 0")]
    public short? unknown5;
    [ExpectedValue(0, "Notice: Was always 0")]
    public short? unknown6;
    public string weaponType;

    public float targetX;
    public float targetY;
    public float attackerX;
    public float attackerY;

    public void Deserialize(RelicBlobReader reader)
    {
        timestamp = reader.ReadSingle();

        targetPlayerId = reader.ReadInt32();
        targetEntityId = reader.ReadInt32();
        hasTarget = reader.ReadByte();
        targetUnitType = reader.ReadPrefixedString();
        unknown3 = reader.ReadInt16();
        unknown4 = reader.ReadInt16();

        attackerPlayerId = reader.ReadInt32();
        attackerEntityId = reader.ReadInt32();
        hasAttacker = reader.ReadByte();
        attackerUnitType = reader.ReadPrefixedString();
        if (hasAttacker != 0)
        {
            unknown5 = reader.ReadInt16();
            unknown6 = reader.ReadInt16();
            weaponType = reader.ReadPrefixedString();
        }

        targetX = reader.ReadSingle();
        targetY = reader.ReadSingle();
        attackerX = reader.ReadSingle();
        attackerY = reader.ReadSingle();
    }

    public override string ToString()
    {
        return $"{timestamp}: {targetEntityId}:{targetUnitType} [{targetX}, {targetY}]";
    }
}

public class DataSTLS : DataModelBase, IDeserializable
{
    [ExpectedValue(0, "Notice: Was always 0")]
    public byte unknown1;
    [ExpectedValue(1, "Notice: Was always 1")]
    public int unknown2;
    public int unknown3;
    public int unknown4;
    [ExpectedValue(0, "Notice: Was always 0")]
    public int unknown5;
    public DataSTLSCreatedEntity[] createdEntities;
    public DataSTLSLostEntity[]    lostEntities;
        
    [ExpectedValue(0, "Warning: possibly another list")]
    public int unknown8;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(2003);

        unknown1 = reader.ReadByte();
        unknown2 = reader.ReadInt32();
        unknown3 = reader.ReadInt32();
        unknown4 = reader.ReadInt32();
        unknown5 = reader.ReadInt32();
        createdEntities = reader.ReadPrefixedArray<DataSTLSCreatedEntity>();
        lostEntities = reader.ReadPrefixedArray<DataSTLSLostEntity>();
        unknown8 = reader.ReadInt32();
    }
}
