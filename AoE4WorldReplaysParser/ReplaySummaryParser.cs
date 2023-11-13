using System.Text;
using AoE4WorldReplaysParser.Models;
using AOEMods.Essence.Chunky.Graph;

namespace AoE4WorldReplaysParser;


public class ReplaySummaryParser : ReplayParserBase
{
    public Dictionary<Type, List<object>> AllStructs { get; set; } = new Dictionary<Type, List<object>>();

    public ReplaySummary Summary { get; private set; }

    private ReplaySummaryPlayer _currentPlayer;
    private string _lastPlayerIdentifier;

    public ReplaySummaryParser()
    {
        Summary = new ReplaySummary();
    }

    public ReplaySummaryParser(Stream stream, string name)
        : base(stream, name)
    {
        Summary = new ReplaySummary();
    }

    public void Load(Stream stream, string name)
    {
        _stream = stream;
        _name = name;

        Summary = new ReplaySummary();
    }

    public static bool IsReplaySummaryFile(Stream stream)
    {
        var pos = stream.Position;
        var magic = new byte[14];
        stream.Read(magic);
        stream.Position = pos;

        return Encoding.Default.GetString(magic) == "Relic Chunky\r\n";

    }

    public override void Parse()
    {
        if (_stream == null || _name == null)
            throw new InvalidOperationException("Need stream and name to parse replay");

        if (!IsReplaySummaryFile(_stream))
            throw new InvalidOperationException("File is not a replay summary file");

        ChunkyFile replay = ChunkyFile.FromStream(_stream);

        foreach (var dataNode in EnumerateNode(replay.RootNodes))
        {
            var path = string.IsNullOrEmpty(dataNode.Header.Path) ? dataNode.Header.Name : $"{dataNode.Header.Path}:{dataNode.Header.Name}";

            var blobStream = new MemoryStream(dataNode.GetData().ToArray());
            var blobReader = new RelicBlobReader(_name, path, blobStream);
            blobReader.Header = dataNode.Header;
            blobReader.StructVersion = dataNode.Header.Version;
            blobReader.NewStructCallback = NewStructHandler;

            switch (path)
            {
                case "STLS":
                    Summary.DataSTLS = blobReader.ReadStruct<DataSTLS>();
                    break;

                case "STPD":
                    var stpd = blobReader.ReadStruct<DataSTPD>();
                    _currentPlayer = new ReplaySummaryPlayer { PlayerDetails = stpd };
                    Summary.Players.Add(_currentPlayer);
                    _lastPlayerIdentifier = stpd.Identifier;
                    break;

                case "STLU":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.Units.Add(blobReader.ReadStruct<DataSTLU>());
                    break;

                case "STLB":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.Buildings.Add(blobReader.ReadStruct<DataSTLB>());
                    break;

                case "STLP":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.Abilities.Add(blobReader.ReadStruct<DataSTLP>());
                    break;

                case "STLC":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.Conversions.Add(blobReader.ReadStruct<DataSTLC>());
                    break;

                case "STLA":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.DataSTLA.Add(blobReader.ReadStruct<DataSTLA>());
                    break;

                case "STDD":
                    blobReader.Identifier = _lastPlayerIdentifier;
                    _currentPlayer!.DataSTDD.Add(blobReader.ReadStruct<DataSTDD>());
                    break;

                default:
                    break;
            }
        }
    }

    private void NewStructHandler(object item)
    {
        if (!AllStructs.TryGetValue(item.GetType(), out var list))
        {
            AllStructs[item.GetType()] = list = new List<object>();
        }

        list.Add(item);
    }

    public void GenerateReport(string rootPath)
    {
        foreach (var pair in AllStructs)
        {
            var path = Path.Combine(rootPath, $"{pair.Key.Name}.csv");

            using (var writer = new StreamWriter(path))
            {
                var fields = pair.Key.GetFields();
                var headers = new List<string>();
                if (pair.Key.IsAssignableTo(typeof(DataModelBase)))
                {
                    headers.Add("File");
                    headers.Add("Identifier");
                }
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(int[]))
                    {
                        var fieldValue = field.GetValue(pair.Value[0]) as int[];
                        if (fieldValue != null)
                        {
                            var len = fieldValue.Length;
                            for (var i = 0; i < len; i++)
                            {
                                headers.Add($"{field.Name}[{i}]");
                            }
                        }
                    }
                    else
                    {
                        headers.Add(field.Name);
                    }
                }

                writer.WriteLine(string.Join(';', headers));

                foreach (var row in pair.Value)
                {
                    var values = new List<string>();

                    if (pair.Key.IsAssignableTo(typeof(DataModelBase)))
                    {
                        values.Add(((DataModelBase)row).File);
                        values.Add(((DataModelBase)row).Identifier);
                    }

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(int[]))
                        {
                            var fieldValue = field.GetValue(row) as int[];
                            if (fieldValue != null)
                            {
                                for (var i = 0; i < fieldValue.Length; i++)
                                {
                                    values.Add(Convert.ToString(fieldValue[i]));
                                }
                            }
                        }
                        else
                        {
                            var fieldValue = field.GetValue(row);
                            values.Add(Convert.ToString(fieldValue)!);
                        }
                    }

                    writer.WriteLine(string.Join(';', values));
                }
            }
        }
    }
}
