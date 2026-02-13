using System;
using System.Collections.Generic;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    internal class TaggerManager
    {
        private readonly IDictionary<string, LinterTagger> taggers = new Dictionary<string, LinterTagger>(StringComparer.OrdinalIgnoreCase);

        internal IEnumerable<LinterTagger> Values => this.taggers.Values;

        internal void Add(LinterTagger tagger)
        {
            this.taggers.Add(tagger.FilePath, tagger);
        }

        internal bool Exists(string filePath)
        {
            return this.taggers.ContainsKey(filePath);
        }

        internal void Remove(LinterTagger tagger)
        {
            this.taggers.Remove(tagger.FilePath);
        }

        internal void Rename(string oldPath, string newPath)
        {
            if (!this.taggers.TryGetValue(oldPath, out var tagger))
            {
                return;
            }

            this.taggers.Add(newPath, tagger);
            this.taggers.Remove(oldPath);
        }

        internal bool TryGetValue(string filePath, out LinterTagger tagger)
        {
            return this.taggers.TryGetValue(filePath, out tagger);
        }
    }
}
