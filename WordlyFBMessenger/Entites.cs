using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace WordlyFBMessenger
{
    public class WordDefinitionCacheEntry : TableEntity
    {
        public string XmlDefinition { get; set; }
        public string AudioFiles { get; set; }
    }

    public class DefinitionEntry
    {
        public string Definition { get; set; }
        public string Usage { get; set; }
    }

    public class WordDefinitions
    {
        public string Word { get; set; }
        public IEnumerable<DefinitionEntry> Entreis { get; set; }
        public IEnumerable<string> AudioFiles { get; set; }
    }
}
