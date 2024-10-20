#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class DataSTPDResourceEntry : DataModelBase, IDeserializable
{
    public int timestamp;
    public ResourceDict current;
    public ResourceDict perMinute;
    public ResourceDict units;
    public ResourceDict cumulative;
    public int unknown1;

    public void Deserialize(RelicBlobReader reader)
    {
        timestamp = reader.ReadInt32();
        current = reader.ReadStruct<ResourceDict>();
        perMinute = reader.ReadStruct<ResourceDict>();
        units = reader.ReadStruct<ResourceDict>();

        // Before 12.0.1974 there was a int32 0-2 here, we called it unknown1. Since 1974 there's another RecourceDict, with _another_ int32 0 after it. I think they inserted one ResourceDict without proper versioning.
        // Regardless, the STPD version remained 2033, so we have to use this behavior to detect the change and hope unknown1 never was >= 9. (9 is the number of resources in the dict, was 8 before olive oil)
        if (reader.PeekInt32() >= 9)
        {
            cumulative = reader.ReadStruct<ResourceDict>();
        }
        unknown1 = reader.ReadInt32();
    }
}

public class DataSTPDScoreEntry : DataModelBase, IDeserializable
{
    public int timestamp;
    public float economy;
    public float military;
    public float society;
    public float technology;
    public float total;

    public void Deserialize(RelicBlobReader reader)
    {
        timestamp = reader.ReadInt32();
        economy = reader.ReadSingle();
        military = reader.ReadSingle();
        society = reader.ReadSingle();
        technology = reader.ReadSingle();
        total = reader.ReadSingle();
    }
}

public class DataSTPDUnitEntry : DataModelBase, IDeserializable
{
    public byte idtype;
    public int? pbgid;
    public byte[] modid;
    public short unknown2;
    public short unknown3;
    public int timestamp;
    public string unitIcon;
    public string unitLabel;
    public string unknown4;
    public ResourceDict unknownResources;
    public string unknown5;
    public int unknown6;

    public void Deserialize(RelicBlobReader reader)
    {
        idtype = reader.ReadByte();
        if (idtype == 1)
            pbgid = reader.ReadInt32();
        else if (idtype == 2)
            modid = reader.ReadBytes(20);
        unknown2 = reader.ReadInt16();
        unknown3 = reader.ReadInt16();
        timestamp = reader.ReadInt32();
        unitIcon = reader.ReadPrefixedString();
        unitLabel = reader.ReadPrefixedUnicodeString();
        unknown4 = reader.ReadPrefixedUnicodeString();
        unknownResources = reader.ReadStruct<ResourceDict>();
        unknown5 = reader.ReadPrefixedUnicodeString();
        unknown6 = reader.ReadInt32();
    }
}

public class DataSTPDUnknownEntry1 : DataModelBase, IDeserializable
{
    public int unknown1;
    public int unknown2;
    public int unknown3;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
        unknown3 = reader.ReadInt32();
    }
}

public class DataSTPDUnknownEntry2 : DataModelBase, IDeserializable
{
    public int unknown1;
    public int unknown2;
    public int unknownPlayerId;
    public float unknown4;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
        unknownPlayerId = reader.ReadInt32();
        unknown4 = reader.ReadSingle();
    }
}

public enum PlayerColor : int
{
    Blue = 0,
    Red = 1,
    Yellow = 2,
    Green = 3,
    Teal = 4,
    Purple = 5,
    Orange = 6,
    Pink = 7
}

public class DataSTPD : DataModelBase, IDeserializable
{
    public int playerId;
    public string playerName;
    public int outcome;
    public int unknown3;
    public int timestampEliminated;
    public int? unknown4;
    public int unknown5a;
    public int unknown5b;
    public int unitsProduced;
    public int unknown5d;
    public int unitsProducedInfantry;
    public int unitsProducedInfantryResources;
    public int unknown5g;
    public int unknown5h;
    public int unknown5i;
    public int unknown5j;
    public int unknown5k;
    public int unknown5l;
    public int largestArmy;
    public int unknown5n;
    public int unknown5o;
    public int unknown5p;
    public int unknown5q;
    public int unknown5r;
    public int unknown5s;
    public int unknown5t;
    public int unknown5u;
    public int unknown5v;
    public int unknown6;
    public int unknown7;
    public ResourceDict unknownItems1;
    public int buildingsLost;
    public int unknown9a;
    public int unitsLost;
    public int unitsLostResources;
    public int unknown9d;
    public int unknown9e;
    public int unknown9f;
    public int unknown9g;
    public int unknown9h;
    public int unknown9i;
    public int techResearched;
    public int unknown9k;
    public ResourceDict unknownItems2a;
    public ResourceDict totalResourcesSpentForUpgrades;
    public ResourceDict unknownItems2c;
    public ResourceDict unknownItems2d;
    public int unitsKilled;
    public int unitsKilledResources;
    public int unknown10c;
    public int unknown10d;
    public int buildingsRazed;
    public int unknown10f;
    public int unknown10g;
    public int unknown10h;
    public int unknown10i;
    public int unknown10j;
    public int unknown10k;
    public ResourceDict totalResourcesGathered;
    public ResourceDict totalResourcesSpent;
    public ResourceDict unknownItems3b;
    public ResourceDict unknownItems3c;
    public ResourceDict unknownItems3d;
    public ResourceDict unknownItems3e;
    public int[] unknown11a;
    public int sacredCaptured;
    public int sacredLost;
    public int sacredNeutralized;
    public int[] unknown11e;
    public ResourceDict unknownItems4;
    public int[] unknown12;
    public byte unknown13;
    public string civ;
    public int unknown14a;
    public int unknown14b;
    public int playerProfileId;
    public int unknown14d;
    public DataSTPDResourceEntry[] resourceTimeline;
    public DataSTPDScoreEntry[] scoreTimeline;
    public DataSTPDUnknownEntry1[] unknown15;
    public int unknown16a;
    public int unknown16b;
    public int unknown16c;
    public int unknown16d;

    public DataSTPDUnitEntry[] unitTimeline;
    public DataSTPDUnknownEntry2[] unknown17;

    public int unknown18;
    public int unknown19;
    public int unknown20;
    public int unknown21a;
    public int unknown21b;
    public int unknown21c;
    public int unknown22;
    public int unknown23;
    public float relicsCaptured;
    public float unknown25;
    public int[] unknown26;
    public byte unknown27a;
    public float unknown27b;
    public int unknown28a;
    public float unknown28b;
    public int unknown28c;
    public float unknown28d;
    public float unknown28e;
    public int unknown28f;
    public float unknown28g;
    public int unknown29a;
    public int unknown29b;
    public int unknown29c;
    public int unknown29d;
    public int unknown29e;
    public int unknown29f;
    public int unknown29g;
    public int unknown29h;
    public int age2AvailableTimestampMsec;
    public int age3AvailableTimestampMsec;
    public int age4AvailableTimestampMsec;
    public int unknown29l;
    public int unknown29m;
    public int unknown30a;
    public float unknown30b;
    public int unknown30c;
    public float unknown30d;
    public PlayerColor? playerColor;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(2029, 2030, 2033);

        playerId = reader.ReadInt32();
        playerName = reader.ReadPrefixedUnicodeString();
        Identifier = $"{playerId} - {playerName}";
        outcome = reader.ReadInt32();
        unknown3 = reader.ReadInt32();
        timestampEliminated = reader.ReadInt32();
        if (reader.StructVersion >= 2033)
            unknown4 = reader.ReadInt32();
        unknown5a = reader.ReadInt32();
        unknown5b = reader.ReadInt32();
        unitsProduced = reader.ReadInt32();
        unknown5d = reader.ReadInt32();
        unitsProducedInfantry = reader.ReadInt32();
        unitsProducedInfantryResources = reader.ReadInt32();
        unknown5g = reader.ReadInt32();
        unknown5h = reader.ReadInt32();
        unknown5i = reader.ReadInt32();
        unknown5j = reader.ReadInt32();
        unknown5k = reader.ReadInt32();
        unknown5l = reader.ReadInt32();
        largestArmy = reader.ReadInt32();
        unknown5n = reader.ReadInt32();
        unknown5o = reader.ReadInt32();
        unknown5p = reader.ReadInt32();
        unknown5q = reader.ReadInt32();
        unknown5r = reader.ReadInt32();
        unknown5s = reader.ReadInt32();
        unknown5t = reader.ReadInt32();
        unknown5u = reader.ReadInt32();
        unknown5v = reader.ReadInt32();
        unknown6 = reader.ReadInt32();
        unknown7 = reader.ReadInt32();
        unknownItems1 = reader.ReadStruct<ResourceDict>();
        buildingsLost = reader.ReadInt32();
        unknown9a = reader.ReadInt32();
        unitsLost = reader.ReadInt32();
        unitsLostResources = reader.ReadInt32();
        unknown9d = reader.ReadInt32();
        unknown9e = reader.ReadInt32();
        unknown9f = reader.ReadInt32();
        unknown9g = reader.ReadInt32();
        unknown9h = reader.ReadInt32();
        unknown9i = reader.ReadInt32();
        techResearched = reader.ReadInt32();
        unknown9k = reader.ReadInt32();
        unknownItems2a = reader.ReadStruct<ResourceDict>();
        totalResourcesSpentForUpgrades = reader.ReadStruct<ResourceDict>();
        unknownItems2c = reader.ReadStruct<ResourceDict>();
        unknownItems2d = reader.ReadStruct<ResourceDict>();
        unitsKilled = reader.ReadInt32();
        unitsKilledResources = reader.ReadInt32();
        unknown10c = reader.ReadInt32();
        unknown10d = reader.ReadInt32();
        buildingsRazed = reader.ReadInt32();
        unknown10f = reader.ReadInt32();
        unknown10g = reader.ReadInt32();
        unknown10h = reader.ReadInt32();
        unknown10i = reader.ReadInt32();
        unknown10j = reader.ReadInt32();
        unknown10k = reader.ReadInt32();
        totalResourcesGathered = reader.ReadStruct<ResourceDict>();
        totalResourcesSpent = reader.ReadStruct<ResourceDict>();
        unknownItems3b = reader.ReadStruct<ResourceDict>();
        unknownItems3c = reader.ReadStruct<ResourceDict>();
        unknownItems3d = reader.ReadStruct<ResourceDict>();
        unknownItems3e = reader.ReadStruct<ResourceDict>();
        unknown11a = reader.ReadInt32Array(6);
        sacredCaptured = reader.ReadInt32();
        sacredLost = reader.ReadInt32();
        sacredNeutralized = reader.ReadInt32();
        unknown11e = reader.ReadInt32Array(9);
        unknownItems4 = reader.ReadStruct<ResourceDict>();
        unknown12 = reader.ReadInt32Array(4);
        unknown13 = reader.ReadByte();
        civ = reader.ReadPrefixedString();
        unknown14a = reader.ReadInt32();
        unknown14b = reader.ReadInt32();
        playerProfileId = reader.ReadInt32();
        unknown14d = reader.ReadInt32();
        resourceTimeline = reader.ReadPrefixedArray<DataSTPDResourceEntry>();
        scoreTimeline = reader.ReadPrefixedArray<DataSTPDScoreEntry>();
        unknown15 = reader.ReadPrefixedArray<DataSTPDUnknownEntry1>();
        unknown16a = reader.ReadInt32();
        unknown16b = reader.ReadInt32();
        unknown16c = reader.ReadInt32();
        unknown16d = reader.ReadInt32();

        unitTimeline = reader.ReadPrefixedArray<DataSTPDUnitEntry>();
        unknown17 = reader.ReadPrefixedArray<DataSTPDUnknownEntry2>();

        unknown18 = reader.ReadInt32();
        unknown19 = reader.ReadInt32();
        unknown20 = reader.ReadInt32();
        unknown21a = reader.ReadInt32();
        unknown21b = reader.ReadInt32();
        unknown21c = reader.ReadInt32();
        unknown22 = reader.ReadInt32();
        unknown23 = reader.ReadInt32();
        relicsCaptured = reader.ReadSingle();
        unknown25 = reader.ReadSingle();
        unknown26 = reader.ReadInt32Array(6);
        unknown27a = reader.ReadByte();
        unknown27b = reader.ReadSingle();
        unknown28a = reader.ReadInt32();
        unknown28b = reader.ReadSingle();
        unknown28c = reader.ReadInt32();
        unknown28d = reader.ReadSingle();
        unknown28e = reader.ReadSingle();
        unknown28f = reader.ReadInt32();
        unknown28g = reader.ReadSingle();
        unknown29a = reader.ReadInt32();
        unknown29b = reader.ReadInt32();
        unknown29c = reader.ReadInt32();
        unknown29d = reader.ReadInt32();
        unknown29e = reader.ReadInt32();
        unknown29f = reader.ReadInt32();
        unknown29g = reader.ReadInt32();
        unknown29h = reader.ReadInt32();
        age2AvailableTimestampMsec = reader.ReadInt32();
        age3AvailableTimestampMsec = reader.ReadInt32();
        age4AvailableTimestampMsec = reader.ReadInt32();
        unknown29l = reader.ReadInt32();
        unknown29m = reader.ReadInt32();
        unknown30a = reader.ReadInt32();
        unknown30b = reader.ReadSingle();
        unknown30c = reader.ReadInt32();
        unknown30d = reader.ReadSingle();
        if (reader.StructVersion >= 2030)
            playerColor = (PlayerColor)reader.ReadInt32();
    }
}
