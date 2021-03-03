using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;


namespace PortingAssistantExtensionServer.TextDocumentModels
{
    class CodeFileDocument
    {
        private string _content;

        private bool _isParsed;

        private ImmutableArray<CsSection> _sections = ImmutableArray<CsSection>.Empty;

        private ImmutableDictionary<string, CsValue> _values = ImmutableDictionary<string, CsValue>.Empty;

        private ImmutableArray<Diagnostic> _diagnostics;

        private int _version = 1;

        private readonly DocumentUri documentUri;



        public CodeFileDocument(DocumentUri documentUri)

        {

            this.documentUri = documentUri;

        }



        public void Load(string content)

        {

            _content = content;

            _isParsed = false;

            _version++;

        }



        public int Version => _version;

        public DocumentUri DocumentUri => documentUri;



        public ImmutableArray<Diagnostic> GetDiagnostics()

        {

            EnsureParsed();

            return _diagnostics;

        }



        public string GetText() => _content;



        public IEnumerable<CsValue> GetValues()

        {

            EnsureParsed();

            return _values.Values;

        }



        public IEnumerable<CsSection> GetSections()

        {

            EnsureParsed();

            return _sections;

        }



        public IEnumerable<string> GetKeys()

        {

            EnsureParsed();

            return _values.Keys;

        }



        public void Update(TextDocumentContentChangeEvent[] changes)

        {

            foreach (var change in changes)

            {

                var startIndex = GetIndexAtPosition(change.Range.Start);

                var endIndex = GetIndexAtPosition(change.Range.End);



                var before = startIndex > 0 ? _content.Substring(0, startIndex + 1) : string.Empty;

                var after = endIndex > -1 && endIndex < _content.Length - 1 ? _content.Substring(endIndex + 1) : string.Empty;

                _content = before + change.Text + after;

            }

            // TODO: Demoware, this could be a proper incremental update.

            Load(_content);



        }



        private int GetIndexAtPosition(Position position)

        {

            var line = 0;

            for (var i = 0; i < _content.Length; i++)

            {

                if (_content[i] == '\n' || (i > 1 && (_content[i - 1] == '\r') || (_content[i] == '\n'))) { line++; }

                if (line == position.Line) return i + position.Character;

            }

            return -1;

        }



        public Position GetPositionAtIndex(int index)

        {

            var line = 0;

            var character = 0;

            for (var i = 0; i < index; i++)

            {

                character++;

                if (i + 1 > _content.Length) break;

                if (_content[i] == '\n') { line++; character = 0; }

            }

            return (line, character);

        }



        public object GetItemAtPosition(Position position)

        {

            var section = _sections.FirstOrDefault(z => z.Location.Start.Line == position.Line);

            if (section != null) return section;



            var value = _values.Values.FirstOrDefault(z => z.KeyLocation.Start.Line == position.Line);

            return value;

        }



        public object GetItemAtIndex(int value)

        {

            return GetItemAtPosition(GetPositionAtIndex(value));

        }



        private void EnsureParsed()

        {

            if (_isParsed) return;



            var values = ImmutableDictionary<string, CsValue>.Empty.ToBuilder();

            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();

            var sections = ImmutableArray<CsSection>.Empty.ToBuilder();



            string sectionPrefix = string.Empty;

            var lines = _content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)

            {

                // shameless stolen from https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Ini/src/IniStreamConfigurationProvider.cs

                var rawLine = lines[lineNumber];

                string line = rawLine.Trim();



                // Ignore blank lines

                if (string.IsNullOrWhiteSpace(line))

                {

                    continue;

                }

                // Ignore comments

                if (line[0] == ';' || line[0] == '#' || line[0] == '/')

                {

                    continue;

                }

                // [Section:header]

                if (line[0] == '[')

                {

                    if (line[^1] == ']')

                    {

                        // remove the brackets

                        sectionPrefix = line[1..^1] + ':';

                        sections.Add(new CsSection(line[1..^1], ((lineNumber, 1), (lineNumber, line.Length - 1))));

                    }

                    else

                    {

                        diagnostics.Add(new Diagnostic()

                        {

                            Code = "NINWISH",

                            Message = "Key is not complete",

                            Severity = DiagnosticSeverity.Warning,

                            Range = ((lineNumber, 0), (lineNumber, line.Length)),

                            Source = "NIN"

                        });

                    }

                    continue;

                }



                // key = value OR "value"

                int separator = line.IndexOf('=');

                if (separator < 0)

                {

                    diagnostics.Add(new Diagnostic()

                    {

                        Code = "NINDEEP",

                        Message = "No value provided for key",

                        Severity = DiagnosticSeverity.Error,

                        Range = ((lineNumber, 0), (lineNumber, line.Length)),

                        Source = "NIN",

                    });

                    continue;

                }



                var key = line.Substring(0, separator);

                string combinedKey = sectionPrefix + line.Substring(0, separator).Trim();

                string value = line[(separator + 1)..];



                // Remove quotes

                if (value.Length > 1 && value[0] == '"' && value[^1] == '"')

                {

                    value = value[1..^1];

                }



                var keyLocation = ((lineNumber, 0), (lineNumber, separator));

                if (values.ContainsKey(combinedKey))

                {

                    diagnostics.Add(new Diagnostic()

                    {

                        Code = "NINHURT",

                        Message = "Duplicate key detected",

                        Severity = DiagnosticSeverity.Error,

                        Range = keyLocation,

                        Source = "NIN",

                    });

                }



                var ninValue = new CsValue(string.IsNullOrEmpty(sectionPrefix) ? sectionPrefix : sectionPrefix[0..^1], key, keyLocation, value, ((lineNumber, separator + 1), (lineNumber, line.Length - 1)));

                values.Add(values.ContainsKey(combinedKey) ? Guid.NewGuid().ToString() : combinedKey, ninValue);



                if (value.Length > 0 && char.IsWhiteSpace(value[0]))

                {

                    diagnostics.Add(new Diagnostic()

                    {

                        Code = "NINWHSP",

                        Message = "Remove leading whitespace",

                        Severity = DiagnosticSeverity.Information,

                        Range = (ninValue.ValueLocation.Start, (lineNumber, ninValue.ValueLocation.Start.Character + value.Length - value.Trim().Length)),

                        Source = "NIN",

                    });

                }

            }



            _diagnostics = diagnostics.ToImmutable();

            _values = values.ToImmutable();

            _sections = sections.ToImmutable();

            _isParsed = true;
        }
    }
    public class CsValue
    {
        public string Section;
        public string Key;
        public string Value;
        public Range KeyLocation;
        public Range ValueLocation;
        public CsValue(string Section, string Key, Range KeyLocation, string Value, Range ValueLocation)
        {
            this.Section = Section;
            this.Key = Key;
            this.Value = Value;
            this.KeyLocation = KeyLocation;
            this.ValueLocation = ValueLocation;
        }
    }
    public class CsSection
    {
        public string Section;
        public Range Location;
        public CsSection(string Section, Range Location)
        {
            this.Section = Section;
            this.Location = Location;
        }

    }
}
