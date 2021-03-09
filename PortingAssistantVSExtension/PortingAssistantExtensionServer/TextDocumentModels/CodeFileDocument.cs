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
        public string GetText() => _content;

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

        private void EnsureParsed()
        {
            if (_isParsed) return;
            var values = ImmutableDictionary<string, CsValue>.Empty.ToBuilder();
            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();
            var sections = ImmutableArray<CsSection>.Empty.ToBuilder();
            //TODO parse code actions to diagnostics

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
